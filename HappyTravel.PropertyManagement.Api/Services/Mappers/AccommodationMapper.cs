using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Contracts = HappyTravel.EdoContracts.Accommodations;
using HappyTravel.EdoContracts.Accommodations.Internals;
using HappyTravel.PropertyManagement.Api.Infrastructure;
using HappyTravel.PropertyManagement.Api.Models.Mappers;
using HappyTravel.PropertyManagement.Api.Models.Mappers.Enums;
using HappyTravel.PropertyManagement.Data;
using HappyTravel.PropertyManagement.Data.Models;
using HappyTravel.PropertyManagement.Data.Models.Accommodations;
using LocationNameNormalizer.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NetTopologySuite.Geometries;
using NetTopologySuite.Index.Strtree;
using Newtonsoft.Json;
using AccommodationDetails = HappyTravel.EdoContracts.Accommodations;

namespace HappyTravel.PropertyManagement.Api.Services.Mappers
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
        public AccommodationMapper(NakijinContext context, IAccommodationsTreesCache treesCache,
            ILoggerFactory loggerFactory, IAccommodationService accommodationService, IOptions<AccommodationsPreloaderOptions> options)
        {
            _context = context;
            _treesCache = treesCache;
            _logger = loggerFactory.CreateLogger<AccommodationMapper>();
            _accommodationService = accommodationService;
            _batchSize = options.Value.BatchSize;
        }


        public async Task MapAccommodations(Suppliers supplier, CancellationToken cancellationToken)
        {
            try
            {
                await ConstructCountryAccommodationsTrees();

                foreach (var countryCode in await GetCountries())
                {
                    var accommodationDetails = new List<Contracts.Accommodation>();
                    int skip = 0;
                    do
                    {
                        accommodationDetails = await GetAccommodationsForMapping(countryCode, supplier, skip, _batchSize, cancellationToken);
                        skip += accommodationDetails.Count;
                        await Map(accommodationDetails, supplier, cancellationToken);
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


        private async Task Map(List<Contracts.Accommodation> accommodations,
            Suppliers supplier, CancellationToken cancellationToken)
        {
            var accommodationsToAdd = new List<RichAccommodationDetails>();
            var accommodationsToUpdate = new List<RichAccommodationDetails>();

            foreach (var accommodation in accommodations)

            {
                var normalized = Normalize(accommodation);

                var nearestAccommodations = await GetNearest(normalized);
                if (!nearestAccommodations.Any())
                {
                    Add(normalized);
                    continue;
                }

                var (matchingResult, score, matchedAccommodation) = Match(nearestAccommodations, normalized);

                switch (matchingResult)
                {
                    case MatchingResults.NotMatch:
                        Add(normalized);
                        break;
                    case MatchingResults.Uncertain:
                        await AddUncertainMatches(normalized, supplier, matchedAccommodation.HtId, score, cancellationToken);
                        break;
                    case MatchingResults.Match:
                        Update(normalized, matchedAccommodation);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(matchingResult));
                }
            }


            void Add(Contracts.Accommodation accommodation)
            {
                var dbAccommodation = new RichAccommodationDetails();
                dbAccommodation.CountryCode = accommodation.Location.CountryCode;
                dbAccommodation.CalculatedAccommodation = accommodation;
                dbAccommodation.SupplierAccommodationCodes.Add(supplier, accommodation.Id);
                dbAccommodation.IsCalculated = true;
                accommodationsToAdd.Add(dbAccommodation);
            }


            void Update(Contracts.Accommodation accommodation, AccommodationKeyData matchedAccommodation)
            {
                var dbAccommodation = new RichAccommodationDetails
                {
                    Id = matchedAccommodation.HtId,
                    SupplierAccommodationCodes = matchedAccommodation.SupplierAccommodationCodes
                };
                if (matchedAccommodation.SupplierAccommodationCodes.All(s => s.Key != supplier))
                {
                    dbAccommodation.SupplierAccommodationCodes.Add(supplier, accommodation.Id);
                    dbAccommodation.IsCalculated = false;
                    accommodationsToUpdate.Add(dbAccommodation);
                }
            }


            _context.AddRange(accommodationsToAdd);
            foreach (var accommodation in accommodationsToUpdate)
            {
                _context.Accommodations.Attach(accommodation);
                _context.Entry(accommodation).Property(p => p.SupplierAccommodationCodes).IsModified = true;
                _context.Entry(accommodation).Property(p => p.IsCalculated).IsModified = true;
            }

            await _context.SaveChangesAsync(cancellationToken);
        }


        private async Task<List<Contracts.Accommodation>> GetAccommodationsForMapping(string countryCode,
            Suppliers supplier, int skip, int take, CancellationToken cancellationToken)
        {
            var accommodations = await (from ac in _context.RawAccommodations
                where ac.Supplier == supplier
                    && ac.CountryCode == countryCode
                select ac).OrderBy(ac => ac.Id).Skip(skip).Take(take).ToListAsync(cancellationToken);

            return accommodations.Select(ac
                    => JsonConvert.DeserializeObject<Contracts.Accommodation>(ac.Accommodation.RootElement.ToString()))
                .ToList();
        }


        private async Task<List<AccommodationKeyData>> GetNearest(Contracts.Accommodation accommodation)
        {
            var tree = await _treesCache.Get(accommodation.Location.CountryCode);

            if (tree == default)
                return new List<AccommodationKeyData>();

            var accommodationEnvelope = new Envelope(accommodation.Location.Coordinates.Longitude - 0.01,
                accommodation.Location.Coordinates.Longitude + 0.01,
                accommodation.Location.Coordinates.Latitude - 0.01, accommodation.Location.Coordinates.Latitude + 0.01);
            return tree.Query(accommodationEnvelope).ToList();
        }


        private (MatchingResults results, float score, AccommodationKeyData keyData) Match(
            List<AccommodationKeyData> nearestAccommodations,
            in Contracts.Accommodation accommodation)
        {
            var results = new List<(AccommodationKeyData accommodationKeyData, float score)>(nearestAccommodations.Count);
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


        private Contracts.Accommodation Normalize(in Contracts.Accommodation accommodation)
            => new Contracts.Accommodation
            (
                accommodation.Id,
                accommodation.Name.ToNormalizedName(),
                accommodation.AccommodationAmenities,
                accommodation.AdditionalInfo,
                accommodation.Category,
                accommodation.Contacts,
                new LocationInfo(
                    accommodation.Location.CountryCode,
                    accommodation.Location.Country,
                    accommodation.Location.LocalityCode,
                    accommodation.Location.Locality,
                    accommodation.Location.LocalityZoneCode,
                    accommodation.Location.LocalityZone,
                    accommodation.Location.Coordinates,
                    accommodation.Location.Address.ToNormalizedName(),
                    accommodation.Location.LocationDescriptionCode,
                    accommodation.Location.PointsOfInterests
                ),
                accommodation.Photos,
                accommodation.Rating,
                accommodation.Schedule,
                accommodation.TextualDescriptions,
                accommodation.Type
            );


        private async Task ConstructCountryAccommodationsTrees()
        {
            foreach (var countryCode in await GetCountries())
            {
                var countryAccommodations = new List<AccommodationKeyData>();
                var accommodations = new List<AccommodationKeyData>();
                var skip = 0;
                do
                {
                     accommodations =
                        await _context.Accommodations.Where(ac => ac.CountryCode == countryCode)
                            .OrderBy(ac => ac.Id)
                            .Skip(skip)
                            .Take(_batchSize)
                            .Select(ac => new AccommodationKeyData
                                {HtId = ac.Id, Data = ac.CalculatedAccommodation, SupplierAccommodationCodes = ac.SupplierAccommodationCodes})
                            .ToListAsync();
                    if (!accommodations.Any())
                        break;

                    skip += _batchSize;
                    countryAccommodations.AddRange(accommodations);
                } while (accommodations.Count > 0);

                var tree = new STRtree<AccommodationKeyData>(countryAccommodations.Count);
                foreach (var ac in countryAccommodations)
                {
                    tree.Insert(new Point(ac.Data.Location.Coordinates.Longitude,
                        ac.Data.Location.Coordinates.Latitude).EnvelopeInternal, ac);
                }

                tree.Build();
                await _treesCache.Set(countryCode, tree);
            }
        }


        private async Task<int> Add(Contracts.Accommodation accommodation, Suppliers supplier,
            CancellationToken cancellationToken)
        {
            var dbAccommodation = new RichAccommodationDetails();
            dbAccommodation.CountryCode = accommodation.Location.CountryCode;
            dbAccommodation.CalculatedAccommodation = accommodation;
            dbAccommodation.SupplierAccommodationCodes.Add(supplier, accommodation.Id);
            dbAccommodation.IsCalculated = true;

            _context.Accommodations.Add(dbAccommodation);
            await _context.SaveChangesAsync(cancellationToken);

            return dbAccommodation.Id;
        }


        private async Task<int> AddUncertainMatches(Contracts.Accommodation accommodation, Suppliers supplier,
            int existingHtId, float score, CancellationToken cancellationToken)
        {
            var newHtId = await Add(accommodation, supplier, cancellationToken);
            _context.AccommodationUncertainMatches.Add(new AccommodationUncertainMatches
            {
                Score = score,
                ExistingHtId = existingHtId,
                NewHtId = newHtId,
                IsActive = true
            });

            await _context.SaveChangesAsync(cancellationToken);

            return newHtId;
        }


        private async Task<List<string>> GetCountries()
        {
            if (_countries.Any())
                return _countries;

            _countries = await _context.RawAccommodations
                .Select(ac => ac.CountryCode)
                .Distinct()
                .ToListAsync();

            return _countries;
        }


        private readonly int _batchSize;
        private readonly IAccommodationService _accommodationService;
        private readonly ILogger<AccommodationMapper> _logger;
        private readonly IAccommodationsTreesCache _treesCache;
        private static List<string> _countries = new List<string>(0);
        private readonly NakijinContext _context;
    }
}