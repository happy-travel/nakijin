﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HappyTravel.EdoContracts.Accommodations.Internals;
using Contracts = HappyTravel.EdoContracts.Accommodations;
using HappyTravel.Nakijin.Data;
using HappyTravel.Nakijin.Data.Models;
using HappyTravel.Nakijin.Data.Models.Accommodations;
using HappyTravel.Nakijin.Api.Infrastructure;
using HappyTravel.Nakijin.Api.Infrastructure.Logging;
using HappyTravel.Nakijin.Api.Models;
using HappyTravel.Nakijin.Api.Models.Mappers;
using HappyTravel.Nakijin.Api.Models.Mappers.Enums;
using HappyTravel.Nakijin.Api.Models.StaticDataPublications;
using HappyTravel.Nakijin.Api.Services.StaticDataPublication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NetTopologySuite.Geometries;
using NetTopologySuite.Index.Strtree;
using Newtonsoft.Json;
using OpenTelemetry.Trace;

namespace HappyTravel.Nakijin.Api.Services.Workers
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
            MultilingualDataHelper multilingualDataHelper, AccommodationMappingsCache mappingsCache,
            TracerProvider tracerProvider, AccommodationsChangePublisher accommodationsChangePublisher)
        {
            _context = context;
            _logger = loggerFactory.CreateLogger<AccommodationMapper>();
            _batchSize = options.Value.MappingBatchSize;
            _multilingualDataHelper = multilingualDataHelper;
            _tracerProvider = tracerProvider;
            _mappingsCache = mappingsCache;
            _accommodationsChangePublisher = accommodationsChangePublisher;
        }


        public async Task MapAccommodations(List<Suppliers> suppliers, MappingTypes mappingType,
            CancellationToken cancellationToken)
        {
            var currentSpan = Tracer.CurrentSpan;
            var tracer = _tracerProvider.GetTracer(nameof(AccommodationMapper));

            foreach (var supplier in suppliers)
            {
                try
                {
                    using var supplierAccommodationsMappingSpan = tracer.StartActiveSpan(
                        $"{nameof(MapAccommodations)} of {supplier.ToString()}", SpanKind.Internal, currentSpan);

                    _logger.LogMappingAccommodationsStart(
                        $"Started mapping of {supplier.ToString()} accommodations");

                    cancellationToken.ThrowIfCancellationRequested();
                    await MapAccommodations(supplier, mappingType, supplierAccommodationsMappingSpan, tracer,
                        cancellationToken);

                    _logger.LogMappingAccommodationsFinish(
                        $"Finished mapping of {supplier.ToString()} accommodations");

                    await _mappingsCache.Fill();
                    supplierAccommodationsMappingSpan.AddEvent("Reset accommodation mappings cache");

                    _context.DataUpdateHistories.Add(new DataUpdateHistory
                    {
                        Supplier = supplier,
                        Type = DataUpdateTypes.Mapping,
                        UpdateTime = DateTime.UtcNow
                    });

                    await _context.SaveChangesAsync(cancellationToken);
                }
                catch (TaskCanceledException)
                {
                    _logger.LogMappingAccommodationsCancel(
                        $"Mapping accommodations of {supplier.ToString()} was canceled by client request.");
                }
                catch (Exception ex)
                {
                    _logger.LogMappingAccommodationsError(ex);
                }
            }
        }


        private async Task MapAccommodations(Suppliers supplier, MappingTypes mappingType, TelemetrySpan mappingSpan,
            Tracer tracer,
            CancellationToken cancellationToken)
        {
            var htAccommodationMappings = await GetHtAccommodationMappings();

            var lastMappingDate = mappingType switch
            {
                MappingTypes.Full => DateTime.MinValue,
                MappingTypes.Incremental => await GetLastMappingDate(supplier, cancellationToken),
                _ => throw new NotSupportedException()
            };

            foreach (var country in await GetCountries(supplier))
            {
                using var countryAccommodationsMappingSpan =
                    tracer.StartActiveSpan($"{nameof(MapAccommodations)} of country with code {country.Code}",
                        SpanKind.Internal, mappingSpan);

                _logger.LogMappingAccommodationsOfSpecifiedCountryStart(
                    $"Started mapping of {supplier.ToString()} accommodations of country with code {country.Code}");

                var countryAccommodationsTree = await GetCountryAccommodationsTree(country.Code, supplier);
                countryAccommodationsMappingSpan.AddEvent("Constructed country accommodations tree");

                var countryAccommodationsOfSupplier = await GeCountryAccommodationBySupplier(country.Code, supplier);

                var notActiveCountryAccommodationsOfSupplier = countryAccommodationsOfSupplier
                    .Where(ac => !ac.AccommodationKeyData.IsActive)
                    .ToDictionary(ac => ac.SupplierCode, ac => ac.AccommodationKeyData);

                var activeCountryAccommodationsOfSupplier = countryAccommodationsOfSupplier
                    .Where(ac => ac.AccommodationKeyData.IsActive)
                    .ToDictionary(ac => ac.SupplierCode, ac => ac.AccommodationKeyData);
                countryAccommodationsMappingSpan.AddEvent("Got supplier's specified country accommodations");

                var activeCountryUncertainMatchesOfSupplier =
                    await GetActiveCountryUncertainMatchesBySupplier(country.Code, supplier, cancellationToken);
                countryAccommodationsMappingSpan.AddEvent("Got supplier's specified country uncertain matches");

                var countryLocalities = await GetLocalitiesByCountry(country.Id);
                var countryLocalityZones = await GetLocalityZonesByCountry(country.Id);
                countryAccommodationsMappingSpan.AddEvent("Got supplier's specified country locations");

                var accommodationDetails = new List<Contracts.MultilingualAccommodation>();
                int skip = 0;
                do
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    accommodationDetails = await GetAccommodationsForMapping(country.Code, supplier, skip,
                        _batchSize, lastMappingDate, cancellationToken);
                    countryAccommodationsMappingSpan.AddEvent(
                        $"Got supplier's specified country accommodations batch skip = {skip}, top = {_batchSize}");

                    skip += accommodationDetails.Count;
                    await Map(country, accommodationDetails, supplier, countryAccommodationsTree,
                        activeCountryAccommodationsOfSupplier, notActiveCountryAccommodationsOfSupplier,
                        activeCountryUncertainMatchesOfSupplier, countryLocalities,
                        countryLocalityZones, htAccommodationMappings, countryAccommodationsMappingSpan,
                        cancellationToken);
                } while (accommodationDetails.Count > 0);

                _logger.LogMappingAccommodationsOfSpecifiedCountryFinish(
                    $"Finished mapping of {supplier.ToString()} accommodations of country with code {country.Code}");
            }
        }


        private async Task Map((string Code, int Id) country,
            List<Contracts.MultilingualAccommodation> accommodationsToMap,
            Suppliers supplier, STRtree<SlimAccommodationData> countryAccommodationsTree,
            Dictionary<string, SlimAccommodationData> activeCountryAccommodationsOfSupplier,
            Dictionary<string, SlimAccommodationData> notActiveCountryAccommodationsOfSupplier,
            List<Tuple<int, int>> activeCountryUncertainMatchesOfSupplier, Dictionary<string, int> countryLocalities,
            Dictionary<(int LocalityId, string LocalityZoneName), int> countryLocalityZones,
            Dictionary<int, (int Id, HashSet<int> MappedHtIds)> htAccommodationMappings,
            TelemetrySpan mappingSpan,
            CancellationToken cancellationToken)
        {
            var removedAccommodations = new List<int>();
            var addedAccommodations = new List<AccommodationData>();

            var accommodationsToAdd = new List<RichAccommodationDetails>();
            var uncertainAccommodationsToAdd = new List<AccommodationUncertainMatches>();
            var utcDate = DateTime.UtcNow;

            foreach (var accommodation in accommodationsToMap)
            {
                var normalized = _multilingualDataHelper.NormalizeAccommodation(accommodation);
                if (normalized.Location.Coordinates.IsEmpty() || !normalized.Location.Coordinates.IsValid())
                {
                    _logger.LogEmptyCoordinatesInAccommodation(
                        $"{supplier.ToString()} have the accommodation with not valid coordinates, which code is {accommodation.SupplierCode}");
                    AddOrChangeActivity(normalized, false);
                    continue;
                }

                // TODO: Try get nearest from db 
                var nearestAccommodations = GetNearest(normalized, countryAccommodationsTree);
                if (!nearestAccommodations.Any())
                {
                    AddOrChangeActivity(normalized, true);
                    continue;
                }

                var (matchingResult, score, matchedAccommodation) = Match(nearestAccommodations, normalized);

                switch (matchingResult)
                {
                    case MatchingResults.NotMatch:
                        AddOrChangeActivity(normalized, true);
                        break;
                    case MatchingResults.Uncertain:
                        AddUncertain(normalized, matchedAccommodation.HtId, score);
                        break;
                    case MatchingResults.Match:
                        Update(normalized, matchedAccommodation);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(matchingResult));
                }
            }

            mappingSpan.AddEvent("Map of accommodations batch");

            var accommodationsFromUncertainToPublish = uncertainAccommodationsToAdd
                .Where(ac => ac.SecondHtId == 0)
                .Select(ac => ac.SecondAccommodation)
                .ToList();

            _context.AddRange(accommodationsToAdd);
            _context.AddRange(uncertainAccommodationsToAdd);
            await _context.SaveChangesAsync(cancellationToken);

            var accommodationsToPublish = accommodationsToAdd
                .Where(a => a.IsActive)
                .Union(accommodationsFromUncertainToPublish);

            foreach (var acc in accommodationsToPublish)
                addedAccommodations.Add(new AccommodationData(acc.Id, acc.KeyData.DefaultName,
                    acc.KeyData.DefaultLocalityName, acc.KeyData.DefaultCountryName, acc.CountryCode,
                    acc.KeyData.Coordinates));

            await _accommodationsChangePublisher.PublishAdded(addedAccommodations);
            await _accommodationsChangePublisher.PublishRemoved(removedAccommodations);

            mappingSpan.AddEvent("Save batch changes to db");

            _context.ChangeTracker.Entries()
                .Where(e => e.Entity != null)
                .Where(e => e.State != EntityState.Detached)
                .ToList()
                .ForEach(e => e.State = EntityState.Detached);


            void AddUncertain(Contracts.MultilingualAccommodation accommodation, int existingHtId, float score)
            {
                int matchedHtId = 0;
                if (activeCountryAccommodationsOfSupplier.TryGetValue(accommodation.SupplierCode, out var existing))
                {
                    matchedHtId = existing.HtId;
                    if (activeCountryUncertainMatchesOfSupplier.Any(eum
                        => eum.Equals(new Tuple<int, int>(matchedHtId, existingHtId))
                        || eum.Equals(new Tuple<int, int>(existingHtId, matchedHtId))))
                    {
                        return;
                    }
                }
                else if (notActiveCountryAccommodationsOfSupplier.TryGetValue(accommodation.SupplierCode,
                    out var existingNotActive))
                {
                    matchedHtId = existingNotActive.HtId;
                    AddOrChangeActivity(accommodation, true);

                    if (activeCountryUncertainMatchesOfSupplier.Any(eum
                        => eum.Equals(new Tuple<int, int>(matchedHtId, existingHtId))
                        || eum.Equals(new Tuple<int, int>(existingHtId, matchedHtId))))
                    {
                        return;
                    }
                }

                uncertainAccommodationsToAdd.Add(new AccommodationUncertainMatches
                {
                    Score = score,
                    FirstHtId = existingHtId,
                    SecondHtId = matchedHtId != 0 ? matchedHtId : 0,
                    Created = utcDate,
                    Modified = utcDate,
                    SecondAccommodation = matchedHtId == 0 ? GetDbAccommodation(accommodation, true) : null,
                    IsActive = true
                });
            }


            void AddOrChangeActivity(Contracts.MultilingualAccommodation accommodation, bool isActive)
            {
                if (isActive && activeCountryAccommodationsOfSupplier.ContainsKey(accommodation.SupplierCode))
                    return;

                if (isActive && notActiveCountryAccommodationsOfSupplier.TryGetValue(accommodation.SupplierCode,
                    out var existingNotActive))
                {
                    var accommodationToUpdate = new RichAccommodationDetails
                    {
                        Id = existingNotActive.HtId,
                        IsActive = true,
                        Modified = utcDate
                    };

                    _context.Attach(accommodationToUpdate);
                    _context.Entry(accommodationToUpdate).Property(ac => ac.IsActive).IsModified = true;
                    _context.Entry(accommodationToUpdate).Property(ac => ac.Modified).IsModified = true;

                    var keyData = _multilingualDataHelper.GetAccommodationKeyData(accommodation);

                    addedAccommodations.Add(new AccommodationData(existingNotActive.HtId, keyData.DefaultName,
                        keyData.DefaultLocalityName, keyData.DefaultCountryName, country.Code, keyData.Coordinates));

                    return;
                }

                if (!isActive && notActiveCountryAccommodationsOfSupplier.ContainsKey(accommodation.SupplierCode))
                    return;

                if (!isActive && activeCountryAccommodationsOfSupplier.TryGetValue(accommodation.SupplierCode,
                    out var existingActive))
                {
                    var accommodationToUpdate = new RichAccommodationDetails
                    {
                        Id = existingActive.HtId,
                        IsActive = false,
                        Modified = utcDate
                    };

                    _context.Attach(accommodationToUpdate);
                    _context.Entry(accommodationToUpdate).Property(ac => ac.IsActive).IsModified = true;
                    _context.Entry(accommodationToUpdate).Property(ac => ac.Modified).IsModified = true;

                    removedAccommodations.Add(existingActive.HtId);
                    return;
                }

                var dbAccommodation = GetDbAccommodation(accommodation, isActive);
                accommodationsToAdd.Add(dbAccommodation);
            }


            void Update(Contracts.MultilingualAccommodation accommodation, SlimAccommodationData matchedAccommodation)
            {
                var accommodationToUpdate = new RichAccommodationDetails
                {
                    Id = matchedAccommodation.HtId,
                    Modified = utcDate,
                    IsCalculated = false,
                    SupplierAccommodationCodes = matchedAccommodation.SupplierAccommodationCodes
                };

                if (!accommodationToUpdate.SupplierAccommodationCodes.TryAdd(supplier, accommodation.SupplierCode))
                {
                    _logger.LogSameAccommodationInOneSupplierError(
                        $"{supplier.ToString()} have the same accommodations with codes {matchedAccommodation.SupplierAccommodationCodes[supplier]} and {accommodation.SupplierCode}");
                    AddOrChangeActivity(accommodation, false);
                    return;
                }

                if (_context.ChangeTracker.Entries<RichAccommodationDetails>()
                    .Any(ac => ac.Entity.Id == matchedAccommodation.HtId))
                {
                    var entry = _context.ChangeTracker.Entries<RichAccommodationDetails>()
                        .Single(ac => ac.Entity.Id == matchedAccommodation.HtId);

                    _logger.LogSameAccommodationInOneSupplierError(
                        $"{supplier.ToString()} have the same accommodations with codes {entry.Entity.SupplierAccommodationCodes[supplier]} and {accommodation.SupplierCode}");
                    AddOrChangeActivity(accommodation, false);
                    return;
                }

                _context.Accommodations.Attach(accommodationToUpdate);
                _context.Entry(accommodationToUpdate).Property(p => p.IsCalculated).IsModified = true;
                _context.Entry(accommodationToUpdate).Property(p => p.Modified).IsModified = true;

                if (activeCountryAccommodationsOfSupplier.TryGetValue(accommodation.SupplierCode,
                    out var existingAccommodation))
                {
                    var accommodationToDeactivate = new RichAccommodationDetails
                    {
                        Id = existingAccommodation.HtId,
                        Modified = utcDate,
                        IsActive = false
                    };

                    removedAccommodations.Add(existingAccommodation.HtId);

                    // TODO: merge two manual corrected data 

                    foreach (var supplierCode in existingAccommodation.SupplierAccommodationCodes)
                        accommodationToUpdate.SupplierAccommodationCodes.TryAdd(supplierCode.Key, supplierCode.Value);

                    _context.Accommodations.Attach(accommodationToDeactivate);
                    _context.Entry(accommodationToDeactivate).Property(p => p.IsActive).IsModified = true;
                    _context.Entry(accommodationToDeactivate).Property(p => p.Modified).IsModified = true;

                    AddOrUpdateHtAccommodationMappings(matchedAccommodation.HtId, existingAccommodation.HtId);
                }

                _context.Entry(accommodationToUpdate).Property(p => p.SupplierAccommodationCodes).IsModified = true;

                // TODO: Deactivate  uncertain matches if exist
            }


            void AddOrUpdateHtAccommodationMappings(int htId, int deactivatedHtId)
            {
                var dbHtAccommodationMapping = new HtAccommodationMapping
                {
                    HtId = htId,
                    MappedHtIds = new HashSet<int>() {deactivatedHtId},
                    Modified = utcDate,
                    IsActive = true
                };

                if (htAccommodationMappings.TryGetValue(deactivatedHtId, out var mappingsOfDeactivated))
                {
                    dbHtAccommodationMapping.MappedHtIds.UnionWith(mappingsOfDeactivated.MappedHtIds);
                    var htAccommodationMappingToDeactivate = new HtAccommodationMapping
                    {
                        Id = mappingsOfDeactivated.Id,
                        IsActive = false,
                        Modified = utcDate
                    };
                    _context.Attach(htAccommodationMappingToDeactivate);
                    _context.Entry(htAccommodationMappingToDeactivate).Property(m => m.IsActive).IsModified = true;
                    _context.Entry(htAccommodationMappingToDeactivate).Property(m => m.Modified).IsModified = true;
                }

                if (htAccommodationMappings.TryGetValue(htId, out var mappings))
                {
                    dbHtAccommodationMapping.Id = mappings.Id;
                    dbHtAccommodationMapping.MappedHtIds.UnionWith(mappings.MappedHtIds);

                    _context.Attach(dbHtAccommodationMapping);
                    _context.Entry(dbHtAccommodationMapping).Property(m => m.MappedHtIds).IsModified = true;
                    _context.Entry(dbHtAccommodationMapping).Property(m => m.Modified).IsModified = true;

                    return;
                }

                dbHtAccommodationMapping.Created = utcDate;
                _context.Add(dbHtAccommodationMapping);
            }


            RichAccommodationDetails GetDbAccommodation(Contracts.MultilingualAccommodation accommodation,
                bool isActive)
            {
                var dbAccommodation = new RichAccommodationDetails();
                dbAccommodation.CountryCode = country.Code;
                dbAccommodation.CalculatedAccommodation = accommodation;
                dbAccommodation.SupplierAccommodationCodes.Add(supplier, accommodation.SupplierCode);
                dbAccommodation.Created = utcDate;
                dbAccommodation.Modified = utcDate;
                dbAccommodation.IsCalculated = true;
                dbAccommodation.KeyData = _multilingualDataHelper.GetAccommodationKeyData(accommodation);

                var locationIds = GetLocationIds(accommodation.Location);
                dbAccommodation.CountryId = locationIds.CountryId;
                dbAccommodation.LocalityId = locationIds.LocalityId;
                dbAccommodation.LocalityZoneId = locationIds.LocalityZoneId;
                dbAccommodation.IsActive = isActive;

                return dbAccommodation;
            }


            (int CountryId, int? LocalityId, int? LocalityZoneId) GetLocationIds(MultilingualLocationInfo location)
            {
                int? localityId = null;
                int? localityZoneId = null;
                if (location.Locality != null!)
                {
                    var defaultLocalityName =
                        location.Locality.GetValueOrDefault(Constants.DefaultLanguageCode);

                    if (!defaultLocalityName.IsValid())
                        return (country.Id, localityId, localityZoneId);

                    localityId = countryLocalities[defaultLocalityName];

                    if (location.LocalityZone != null!)
                    {
                        var defaultLocalityZoneName =
                            location.LocalityZone.GetValueOrDefault(Constants.DefaultLanguageCode);

                        if (defaultLocalityZoneName.IsValid())
                            localityZoneId = countryLocalityZones[(localityId.Value, defaultLocalityZoneName)];
                    }
                }

                return (country.Id, localityId, localityZoneId);
            }
        }


        private async Task<List<Contracts.MultilingualAccommodation>> GetAccommodationsForMapping(string countryCode,
            Suppliers supplier, int skip, int take, DateTime lastUpdatedDate, CancellationToken cancellationToken)
        {
            var accommodations = await (from ac in _context.RawAccommodations
                where ac.Supplier == supplier
                    && ac.CountryCode == countryCode
                    && ac.Modified > lastUpdatedDate
                select ac).OrderBy(ac => ac.Id).Skip(skip).Take(take).ToListAsync(cancellationToken);

            return accommodations.Select(ac
                    => JsonConvert.DeserializeObject<Contracts.MultilingualAccommodation>(ac.Accommodation.RootElement
                        .ToString()!))
                .ToList();
        }


        private List<SlimAccommodationData> GetNearest(Contracts.MultilingualAccommodation accommodation,
            STRtree<SlimAccommodationData> tree)
        {
            var accommodationEnvelope = new Envelope(accommodation.Location.Coordinates.Longitude - 0.01,
                accommodation.Location.Coordinates.Longitude + 0.01,
                accommodation.Location.Coordinates.Latitude - 0.01, accommodation.Location.Coordinates.Latitude + 0.01);
            return tree.Query(accommodationEnvelope).ToList();
        }


        private (MatchingResults results, float score, SlimAccommodationData slimData) Match(
            List<SlimAccommodationData> nearestAccommodations,
            in Contracts.MultilingualAccommodation accommodation)
        {
            var results =
                new List<(SlimAccommodationData slimData, float score)>(nearestAccommodations.Count);
            foreach (var nearestAccommodation in nearestAccommodations)
            {
                var score = ComparisonScoreCalculator.Calculate(nearestAccommodation.KeyData,
                    _multilingualDataHelper.GetAccommodationKeyData(accommodation));

                results.Add((nearestAccommodation, score));
            }

            var (slimData, maxScore) = results.Aggregate((r1, r2) => r2.score > r1.score ? r2 : r1);

            if (MatchingMinimumScore <= maxScore)
                return (MatchingResults.Match, maxScore, slimData);

            if (UncertainMatchingMinimumScore <= maxScore && maxScore < MatchingMinimumScore)
                return (MatchingResults.Uncertain, maxScore, slimData);

            return (MatchingResults.NotMatch, maxScore, new SlimAccommodationData());
        }


        private async Task<List<(string SupplierCode, SlimAccommodationData AccommodationKeyData)>>
            GeCountryAccommodationBySupplier(string countryCode, Suppliers supplier)
        {
            var countryAccommodations = new List<SlimAccommodationData>();
            var accommodations = new List<SlimAccommodationData>();
            var skip = 0;
            do
            {
                accommodations =
                    await _context.Accommodations.Where(ac
                            => ac.CountryCode == countryCode && EF.Functions.JsonExists(ac.SupplierAccommodationCodes,
                                supplier.ToString().ToLower()))
                        .OrderBy(ac => ac.Id)
                        .Skip(skip)
                        .Take(_batchSize)
                        .Select(ac => new SlimAccommodationData
                        {
                            HtId = ac.Id,
                            SupplierAccommodationCodes = ac.SupplierAccommodationCodes,
                            IsActive = ac.IsActive
                        })
                        .ToListAsync();

                skip += _batchSize;
                countryAccommodations.AddRange(accommodations);
            } while (accommodations.Count > 0);

            return countryAccommodations.Select(ac => (ac.SupplierAccommodationCodes[supplier], ac)).ToList();
        }


        private async Task<STRtree<SlimAccommodationData>> GetCountryAccommodationsTree(string countryCode,
            Suppliers supplier)
        {
            var countryAccommodations = new List<SlimAccommodationData>();
            var accommodations = new List<SlimAccommodationData>();
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
                        .Select(ac => new SlimAccommodationData
                        {
                            HtId = ac.Id,
                            KeyData = ac.KeyData,
                            SupplierAccommodationCodes = ac.SupplierAccommodationCodes
                        })
                        .ToListAsync();

                skip += _batchSize;
                countryAccommodations.AddRange(accommodations);
            } while (accommodations.Count > 0);

            if (!countryAccommodations.Any() || countryAccommodations.Count == 1)
                return new STRtree<SlimAccommodationData>();

            var tree = new STRtree<SlimAccommodationData>(countryAccommodations.Count);
            foreach (var ac in countryAccommodations)
            {
                if (!ac.KeyData.Coordinates.IsEmpty() && ac.KeyData.Coordinates.IsValid())
                    tree.Insert(new Point(ac.KeyData.Coordinates.Longitude,
                        ac.KeyData.Coordinates.Latitude).EnvelopeInternal, ac);
            }

            tree.Build();
            return tree;
        }


        private async Task<List<(string Code, int Id)>> GetCountries(Suppliers supplier)
        {
            var countries = await _context.Countries
                .Where(c => c.IsActive && EF.Functions.JsonExists(c.SupplierCountryCodes,
                    supplier.ToString().ToLower()))
                .OrderBy(c => c.Code)
                .Select(c => ValueTuple.Create(c.Code, c.Id))
                .ToListAsync();

            return countries;
        }


        private Task<List<Tuple<int, int>>> GetActiveCountryUncertainMatchesBySupplier(string countryCode,
            Suppliers supplier, CancellationToken cancellationToken)
            => (from um in _context.AccommodationUncertainMatches
                join firstAc in _context.Accommodations on um.FirstHtId equals firstAc.Id
                join secondAc in _context.Accommodations on um.SecondHtId equals secondAc.Id
                where um.IsActive && firstAc.CountryCode == countryCode && secondAc.CountryCode == countryCode &&
                    (EF.Functions.JsonExists(firstAc.SupplierAccommodationCodes,
                        supplier.ToString().ToLower()) || EF.Functions.JsonExists(secondAc.SupplierAccommodationCodes,
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


        private Task<Dictionary<int, (int Id, HashSet<int> MappedHtIds)>> GetHtAccommodationMappings()
            => _context.HtAccommodationMappings
                .Where(m => m.IsActive)
                .Select(m => new
                {
                    Id = m.Id,
                    HtId = m.HtId,
                    MappedHtIds = m.MappedHtIds
                })
                .ToDictionaryAsync(m => m.HtId, m => (m.Id, m.MappedHtIds));


        private Task<DateTime> GetLastMappingDate(Suppliers supplier, CancellationToken cancellationToken)
            => _context.DataUpdateHistories.Where(h => h.Supplier == supplier && h.Type == DataUpdateTypes.Mapping)
                .OrderByDescending(h => h.UpdateTime)
                .Select(h => h.UpdateTime).FirstOrDefaultAsync(cancellationToken);


        private readonly int _batchSize;
        private readonly AccommodationsChangePublisher _accommodationsChangePublisher;
        private readonly ILogger<AccommodationMapper> _logger;
        private readonly MultilingualDataHelper _multilingualDataHelper;
        private readonly NakijinContext _context;
        private readonly TracerProvider _tracerProvider;
        private readonly AccommodationMappingsCache _mappingsCache;

        private const float UncertainMatchingMinimumScore = 1.5f;
        private const float MatchingMinimumScore = 3f;
    }
}