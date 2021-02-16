using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Contracts = HappyTravel.EdoContracts.Accommodations;
using HappyTravel.StaticDataMapper.Data;
using HappyTravel.StaticDataMapper.Data.Models;
using HappyTravel.StaticDataMapper.Data.Models.Accommodations;
using HappyTravel.StaticDataMapper.Api.Infrastructure;
using HappyTravel.StaticDataMapper.Api.Models;
using HappyTravel.StaticDataMapper.Api.Models.Mappers;
using HappyTravel.StaticDataMapper.Api.Models.Mappers.Enums;
using LocationNameNormalizer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NetTopologySuite.Geometries;
using NetTopologySuite.Index.Strtree;
using Newtonsoft.Json;

namespace HappyTravel.StaticDataMapper.Api.Services.Workers
{
    /*
        1. Get accommodations by a country
        2. Load existing accommodations from a DB
            a. If no accommodations existing, normalize the accommodation and insert it to the DB
        3. Find nearest neighbors for each new accommodation in 0.01DD radius
        4. Add score for a resulting list:
            * Full match of a normalized name — 2 points
            * Full match of a normalized address — 0.5 points (address formats may be different, that is why small point)
            * Rating match — 0.5 points
            * Contact details match — 0.5 points
           If an accommodation scores less than 1.5 points we consider it not-matching.
           If  an accommodation scores greater or equal to 3 points we consider it matching.
           Intermediate scores should be calibrated to achieve better matching
        5. If the score is sufficient, merge the new and the existing accommodation. Unmatched field became synonyms
    */
    public class AccommodationMapper : IAccommodationMapper
    {
        public AccommodationMapper(NakijinContext context,
            ILoggerFactory loggerFactory, IOptions<StaticDataLoadingOptions> options,
            MultilingualDataNormalizer multilingualDataNormalizer)
        {
            _context = context;
            _logger = loggerFactory.CreateLogger<AccommodationMapper>();
            _batchSize = options.Value.BatchSize;
            _multilingualDataNormalizer = multilingualDataNormalizer;
        }

        public async Task MapAccommodations(Suppliers supplier, CancellationToken cancellationToken)
        {
            try
            {
                foreach (var country in await GetCountries())
                {
                    var accommodationDetails = new List<Contracts.MultilingualAccommodation>();
                    int skip = 0;
                    do
                    {
                        accommodationDetails = await GetAccommodationsForMapping(country.Code, supplier, skip,
                            _batchSize, cancellationToken);
                        skip += accommodationDetails.Count;
                        await Map(country, accommodationDetails, supplier, cancellationToken);
                        cancellationToken.ThrowIfCancellationRequested();
                    } while (accommodationDetails.Count > 0);
                }
            }
            catch (TaskCanceledException)
            {
                // TODO: Use generated logging extension methods
                _logger.Log(LogLevel.Information,
                    $"Mapping accommodations of {supplier.ToString()} was canceled by client request.");
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error,
                    $"Mapping accommodations of {supplier.ToString()} was stopped because of {ex.Message}");
            }
        }


        private async Task Map((string Code, int Id) country, List<Contracts.MultilingualAccommodation> accommodations,
            Suppliers supplier, CancellationToken cancellationToken)
        {
            var accommodationsToAdd = new List<RichAccommodationDetails>();
            var utcDate = DateTime.UtcNow;

            var countryAccommodationsTree = await GetCountryAccommodationsTree(country.Code, supplier);
            var countryAccommodationsBySupplier = await GetCountryAccommodationBySupplier(country.Code, supplier);
            var countryUncertainMatchesBySupplier =
                await GetCountryUncertainMatchesBySupplier(country.Code, supplier, cancellationToken);
            var countryLocalities = await GetLocalitiesByCountry(country.Id);
            var countryLocalityZones = await GetLocalityZonesByCountry(country.Id);

            foreach (var accommodation in accommodations)

            {
                var normalized = _multilingualDataNormalizer.NormalizeAccommodation(accommodation);

                // TODO: Try get nearest from db 
                var nearestAccommodations = GetNearest(normalized, countryAccommodationsTree);
                if (!nearestAccommodations.Any())
                {
                    await AddOrIgnore(normalized);
                    continue;
                }

                var (matchingResult, score, matchedAccommodation) = Match(nearestAccommodations, normalized);

                switch (matchingResult)
                {
                    case MatchingResults.NotMatch:
                        await AddOrIgnore(normalized);
                        break;
                    case MatchingResults.Uncertain:
                        await AddUncertainMatches(normalized, supplier, matchedAccommodation.HtId, score,
                            countryAccommodationsBySupplier, countryUncertainMatchesBySupplier,
                            cancellationToken);
                        break;
                    case MatchingResults.Match:
                        Update(normalized, matchedAccommodation);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(matchingResult));
                }
            }


            async Task AddOrIgnore(Contracts.MultilingualAccommodation accommodation)
            {
                if (countryAccommodationsBySupplier.ContainsKey(accommodation.SupplierCode))
                    return;

                var dbAccommodation = new RichAccommodationDetails();
                dbAccommodation.CountryCode = accommodation.Location.CountryCode;
                dbAccommodation.CalculatedAccommodation = accommodation;
                dbAccommodation.SupplierAccommodationCodes.Add(supplier, accommodation.SupplierCode);
                dbAccommodation.Created = utcDate;
                dbAccommodation.Modified = utcDate;
                dbAccommodation.IsCalculated = true;

                dbAccommodation.CountryId = country.Id;

                if (accommodation.Location.Locality != null!)
                {
                    var defaultLocalityName =
                        accommodation.Location.Locality.GetValueOrDefault(Constants.DefaultLanguageCode);
                    // Can not be exception 
                    var localityId = countryLocalities[defaultLocalityName];
                    dbAccommodation.LocalityId = localityId;

                    if (accommodation.Location.LocalityZone != null!)
                    {
                        var defaultLocalityZoneName =
                            accommodation.Location.LocalityZone.GetValueOrDefault(Constants.DefaultLanguageCode);
                        // Can not be exception 
                        var localityZoneId = countryLocalityZones[(localityId, defaultLocalityName)];
                        dbAccommodation.LocalityZoneId = localityZoneId;
                    }
                }


                accommodationsToAdd.Add(dbAccommodation);
            }


            void Update(Contracts.MultilingualAccommodation accommodation, AccommodationKeyData matchedAccommodation)
            {
                var accommodationToUpdate = new RichAccommodationDetails
                {
                    Id = matchedAccommodation.HtId,
                    Modified = utcDate,
                    IsCalculated = false,
                    SupplierAccommodationCodes = matchedAccommodation.SupplierAccommodationCodes
                };
                _context.Accommodations.Attach(accommodationToUpdate);
                _context.Entry(accommodationToUpdate).Property(p => p.IsCalculated).IsModified = true;
                _context.Entry(accommodationToUpdate).Property(p => p.Modified).IsModified = true;

                if (countryAccommodationsBySupplier.TryGetValue(accommodation.SupplierCode,
                    out var existingAccommodation))
                {
                    var accommodationToDeactivate = new RichAccommodationDetails
                    {
                        Id = existingAccommodation.HtId,
                        Modified = utcDate,
                        IsActive = false
                    };

                    // TODO: merge two manual corrected data 
                    if (!existingAccommodation.Data.Equals(default))
                    {
                        accommodationToUpdate.AccommodationWithManualCorrections = existingAccommodation.Data;
                        _context.Entry(accommodationToUpdate).Property(p => p.AccommodationWithManualCorrections)
                            .IsModified = true;
                    }

                    foreach (var supplierCode in existingAccommodation.SupplierAccommodationCodes)
                        accommodationToUpdate.SupplierAccommodationCodes.TryAdd(supplierCode.Key, supplierCode.Value);

                    _context.Accommodations.Attach(accommodationToDeactivate);
                    _context.Entry(accommodationToDeactivate).Property(p => p.IsActive).IsModified = true;
                    _context.Entry(accommodationToDeactivate).Property(p => p.Modified).IsModified = true;
                }

                _context.Entry(accommodationToUpdate).Property(p => p.SupplierAccommodationCodes).IsModified = true;
            }


            _context.AddRange(accommodationsToAdd);
            await _context.SaveChangesAsync(cancellationToken);

            _context.ChangeTracker.Entries()
                .Where(e => e.Entity != null)
                .Where(e => e.State != EntityState.Detached)
                .ToList()
                .ForEach(e => e.State = EntityState.Detached);
        }


        private async Task<List<Contracts.MultilingualAccommodation>> GetAccommodationsForMapping(string countryCode,
            Suppliers supplier, int skip, int take, CancellationToken cancellationToken)
        {
            var accommodations = await (from ac in _context.RawAccommodations
                where ac.Supplier == supplier
                    && ac.CountryCode == countryCode
                select ac).OrderBy(ac => ac.Id).Skip(skip).Take(take).ToListAsync(cancellationToken);

            return accommodations.Select(ac
                    => JsonConvert.DeserializeObject<Contracts.MultilingualAccommodation>(ac.Accommodation.RootElement
                        .ToString()!))
                .ToList();
        }


        private List<AccommodationKeyData> GetNearest(Contracts.MultilingualAccommodation accommodation,
            STRtree<AccommodationKeyData> tree)
        {
            var accommodationEnvelope = new Envelope(accommodation.Location.Coordinates.Longitude - 0.01,
                accommodation.Location.Coordinates.Longitude + 0.01,
                accommodation.Location.Coordinates.Latitude - 0.01, accommodation.Location.Coordinates.Latitude + 0.01);
            return tree.Query(accommodationEnvelope).ToList();
        }


        private (MatchingResults results, float score, AccommodationKeyData keyData) Match(
            List<AccommodationKeyData> nearestAccommodations,
            in Contracts.MultilingualAccommodation accommodation)
        {
            var results =
                new List<(AccommodationKeyData accommodationKeyData, float score)>(nearestAccommodations.Count);
            foreach (var nearestAccommodation in nearestAccommodations)
            {
                var score = ComparisonScoreCalculator.Calculate(nearestAccommodation.Data, accommodation);

                results.Add((nearestAccommodation, score));
            }

            var (htId, maxScore) = results.Aggregate((r1, r2) => r2.score > r1.score ? r2 : r1);

            if (3 <= Math.Round(maxScore))
                return (MatchingResults.Match, maxScore, htId);

            if (1.5 <= maxScore && maxScore < 3)
                return (MatchingResults.Uncertain, maxScore, htId);

            return (MatchingResults.NotMatch, maxScore, new AccommodationKeyData());
        }
        

        private async Task<Dictionary<string, AccommodationKeyData>> GetCountryAccommodationBySupplier(
            string countryCode, Suppliers supplier)
        {
            var countryAccommodations = new List<AccommodationKeyData>();
            var accommodations = new List<AccommodationKeyData>();
            var skip = 0;
            do
            {
                accommodations =
                    await _context.Accommodations.Where(ac
                            => ac.CountryCode == countryCode && EF.Functions.JsonExists(ac.SupplierAccommodationCodes,
                                supplier.ToString().ToLower())
                            && ac.IsActive)
                        .OrderBy(ac => ac.Id)
                        .Skip(skip)
                        .Take(_batchSize)
                        .Select(ac => new AccommodationKeyData
                        {
                            HtId = ac.Id,
                            Data = ac.AccommodationWithManualCorrections,
                            SupplierAccommodationCodes = ac.SupplierAccommodationCodes
                        })
                        .ToListAsync();


                skip += _batchSize;
                countryAccommodations.AddRange(accommodations);
            } while (accommodations.Count > 0);

            return accommodations.ToDictionary(ac => ac.SupplierAccommodationCodes[supplier],
                ac => ac);
        }

        private async Task<STRtree<AccommodationKeyData>> GetCountryAccommodationsTree(string countryCode,
            Suppliers supplier)
        {
            var countryAccommodations = new List<AccommodationKeyData>();
            var accommodations = new List<AccommodationKeyData>();
            var skip = 0;
            do
            {
                accommodations =
                    await _context.Accommodations.Where(ac
                            => ac.CountryCode == countryCode && !EF.Functions.JsonExists(ac.SupplierAccommodationCodes,
                                supplier.ToString().ToLower()) && ac.IsActive)
                        .OrderBy(ac => ac.Id)
                        .Skip(skip)
                        .Take(_batchSize)
                        .Select(ac => new AccommodationKeyData
                        {
                            HtId = ac.Id,
                            Data = ac.CalculatedAccommodation,
                            SupplierAccommodationCodes = ac.SupplierAccommodationCodes
                        })
                        .ToListAsync();


                skip += _batchSize;
                countryAccommodations.AddRange(accommodations);
            } while (accommodations.Count > 0);

            if (!countryAccommodations.Any() || countryAccommodations.Count == 1)
                return new STRtree<AccommodationKeyData>();

            var tree = new STRtree<AccommodationKeyData>(countryAccommodations.Count);
            foreach (var ac in countryAccommodations)
            {
                tree.Insert(new Point(ac.Data.Location.Coordinates.Longitude,
                    ac.Data.Location.Coordinates.Latitude).EnvelopeInternal, ac);
            }

            tree.Build();
            return tree;
        }


        private async Task<int> Add(Contracts.MultilingualAccommodation accommodation, Suppliers supplier,
            CancellationToken cancellationToken)
        {
            var dbAccommodation = new RichAccommodationDetails();
            var utcDate = DateTime.UtcNow;
            dbAccommodation.CountryCode = accommodation.Location.CountryCode;
            dbAccommodation.CalculatedAccommodation = accommodation;
            dbAccommodation.SupplierAccommodationCodes.Add(supplier, accommodation.SupplierCode);
            dbAccommodation.Created = utcDate;
            dbAccommodation.Modified = utcDate;
            dbAccommodation.IsCalculated = true;

            _context.Accommodations.Add(dbAccommodation);
            await _context.SaveChangesAsync(cancellationToken);

            return dbAccommodation.Id;
        }


        private async Task AddUncertainMatches(Contracts.MultilingualAccommodation accommodation,
            Suppliers supplier, int existingHtId, float score,
            Dictionary<string, AccommodationKeyData> existingCountryAccommodations,
            List<Tuple<int, int>> existingUncertainMatches,
            CancellationToken cancellationToken)
        {
            int firstHtId = 0;
            if (existingCountryAccommodations.TryGetValue(accommodation.SupplierCode, out var existing))
            {
                firstHtId = existing.HtId;
                if (existingUncertainMatches.Any(eum => eum.Equals(new Tuple<int, int>(firstHtId, existingHtId))
                    || eum.Equals(new Tuple<int, int>(existingHtId, firstHtId))))
                    return;
            }
            else
            {
                firstHtId = await Add(accommodation, supplier, cancellationToken);
            }

            var utcDate = DateTime.UtcNow;
            _context.AccommodationUncertainMatches.Add(new AccommodationUncertainMatches
            {
                Score = score,
                FirstHtId = existingHtId,
                SecondHtId = firstHtId,
                Created = utcDate,
                Modified = utcDate,
                IsActive = true
            });

            await _context.SaveChangesAsync(cancellationToken);
        }

        private async Task<List<(string Code, int Id)>> GetCountries()
        {
            if (_countries.Any())
                return _countries;

            _countries = await _context.Countries
                .Select(c => ValueTuple.Create(c.Code, c.Id))
                .ToListAsync();

            return _countries;
        }

        private Task<List<Tuple<int, int>>> GetCountryUncertainMatchesBySupplier(string countryCode,
            Suppliers supplier, CancellationToken cancellationToken)
            => (from um in _context.AccommodationUncertainMatches
                join firstAc in _context.Accommodations on um.FirstHtId equals firstAc.Id
                join secondAc in _context.Accommodations on um.SecondHtId equals secondAc.Id
                where firstAc.CountryCode == countryCode && secondAc.CountryCode == countryCode &&
                    (!EF.Functions.JsonExists(firstAc.SupplierAccommodationCodes,
                        supplier.ToString().ToLower()) || !EF.Functions.JsonExists(secondAc.SupplierAccommodationCodes,
                        supplier.ToString().ToLower()))
                select new Tuple<int, int>(um.FirstHtId, um.SecondHtId)).ToListAsync(cancellationToken);

        private Task<Dictionary<string, int>> GetLocalitiesByCountry(int countryId)
            => _context.Localities.Where(l => l.CountryId == countryId && l.IsActive)
                .Select(l => new {Name = l.Names.En, l.Id}).ToDictionaryAsync(l => l.Name, l => l.Id);


        private Task<Dictionary<(int LocalityId, string LocalityZoneName), int>> GetLocalityZonesByCountry(
            int countryId)
            => (from z in _context.LocalityZones
                join l in _context.Localities on z.LocalityId equals l.Id
                where z.IsActive && l.IsActive && l.CountryId == countryId
                select new
                {
                    LocalityId = l.Id,
                    LocalityZoneName = z.Names.En,
                    Id = z.Id
                }).ToDictionaryAsync(z => (z.LocalityId, z.LocalityZoneName), z => z.Id);


        private readonly int _batchSize;
        private readonly ILogger<AccommodationMapper> _logger;
        private static List<(string Code, int Id)> _countries = new List<(string Code, int Id)>(0);
        private readonly MultilingualDataNormalizer _multilingualDataNormalizer;
        private readonly NakijinContext _context;
    }
}