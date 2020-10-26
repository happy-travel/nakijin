using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Contracts = HappyTravel.EdoContracts.Accommodations;
using HappyTravel.EdoContracts.Accommodations.Internals;
using HappyTravel.PropertyManagement.Api.Infrastructure;
using HappyTravel.PropertyManagement.Api.Models.Mappers.Enums;
using HappyTravel.PropertyManagement.Data;
using HappyTravel.PropertyManagement.Data.Models;
using HappyTravel.PropertyManagement.Data.Models.Accommodations;
using LocationNameNormalizer.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
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
            ILoggerFactory loggerFactory, IAccommodationService accommodationService)
        {
            _context = context;
            _treesCache = treesCache;
            _logger = loggerFactory.CreateLogger<AccommodationMapper>();
            _accommodationService = accommodationService;
        }


        public async Task MapAccommodations(Suppliers supplier, CancellationToken cancellationToken)
        {
            try
            {
                await ConstructCountryAccommodationsTrees();

                foreach (var countryCode in await GetCountries())
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var accommodationDetails =
                        await GetAccommodationsForMapping(countryCode, supplier, cancellationToken);
                    await MapCountry(accommodationDetails, supplier, cancellationToken);
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


        // TODO: Change and use return value
        private async Task<Dictionary<string, int>> MapCountry(List<Contracts.Accommodation> accommodations,
            Suppliers supplier, CancellationToken cancellationToken)
        {
            var results = new Dictionary<string, int>();

            foreach (var accommodation in accommodations)
            {
                var htId = await Map(accommodation, supplier, cancellationToken);
                results.Add(accommodation.Id, htId);
            }

            return results;
        }


        private async Task<int> Map(Contracts.Accommodation accommodation, Suppliers supplier,
            CancellationToken cancellationToken)
        {
            var normalized = Normalize(accommodation);

            var nearestAccommodations = await GetNearest(normalized);
            if (!nearestAccommodations.Any())
                return await Add(normalized, supplier, cancellationToken);

            var (matchingResult, score, htId) = Match(nearestAccommodations, normalized);

            return matchingResult switch
            {
                MatchingResults.NotMatch => await Add(normalized, supplier, cancellationToken),
                MatchingResults.Uncertain => await AddUncertainMatches(normalized, supplier, htId, score,
                    cancellationToken),
                MatchingResults.Match => await Update(htId, normalized.Id, supplier, cancellationToken),
                _ => throw new ArgumentOutOfRangeException(nameof(matchingResult))
            };
        }


        private async Task<List<Contracts.Accommodation>> GetAccommodationsForMapping(string countryCode,
            Suppliers supplier,
            CancellationToken cancellationToken)
        {
            var accommodations = await (from ac in _context.RawAccommodations
                where ac.Supplier == supplier
                    && ac.CountryCode == countryCode
                select ac).ToListAsync(cancellationToken);

            return accommodations.Select(ac
                    => JsonConvert.DeserializeObject<Contracts.Accommodation>(ac.Accommodation.RootElement.ToString()))
                .ToList();
        }


        private async Task<List<KeyValuePair<int, Contracts.Accommodation>>> GetNearest(Contracts.Accommodation accommodation)
        {
            var tree = await _treesCache.Get(accommodation.Location.CountryCode);

            if (tree == default)
                return new List<KeyValuePair<int, Contracts.Accommodation>>();

            var accommodationEnvelope = new Envelope(accommodation.Location.Coordinates.Longitude - 0.01,
                accommodation.Location.Coordinates.Longitude + 0.01,
                accommodation.Location.Coordinates.Latitude - 0.01, accommodation.Location.Coordinates.Latitude + 0.01);
            return tree.Query(accommodationEnvelope).ToList();
        }


        private (MatchingResults results, float score, int htId) Match(
            List<KeyValuePair<int, Contracts.Accommodation>> nearestAccommodations,
            in Contracts.Accommodation accommodation)
        {
            var results = new List<(int HtId, float Score)>(nearestAccommodations.Count);
            foreach (var nearestAccommodation in nearestAccommodations)
            {
                var score = ComparisonScoreCalculator.Calculate(nearestAccommodation.Value, accommodation);

                results.Add((nearestAccommodation.Key, score));
            }

            var (htId, maxScore) = results.Aggregate((r1, r2) => r2.Score > r1.Score ? r2 : r1);

            if (3 <= maxScore)
                return (MatchingResults.Match, maxScore, htId);

            if (1.5 <= maxScore && maxScore < 3)
                return (MatchingResults.Uncertain, maxScore, htId);

            return (MatchingResults.NotMatch, maxScore, 0);
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
                var accommodations =
                    await _context.Accommodations.Where(ac => ac.CountryCode == countryCode)
                        .Select(ac => new KeyValuePair<int, Contracts.Accommodation>(ac.Id, ac.CalculatedAccommodation))
                        .ToListAsync();
                if (!accommodations.Any())
                    continue;

                var tree = new STRtree<KeyValuePair<int, Contracts.Accommodation>>(accommodations.Count);
                foreach (var ac in accommodations)
                {
                    tree.Insert(new Point(ac.Value.Location.Coordinates.Longitude,
                        ac.Value.Location.Coordinates.Latitude).EnvelopeInternal, ac);
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


        private async Task<int> Update(int htId, string supplierAccommodationId, Suppliers supplier,
            CancellationToken cancellationToken)
        {
            var dbAccommodation = await _context.Accommodations.SingleAsync(ac => ac.Id == htId, cancellationToken);
            if (dbAccommodation.SupplierAccommodationCodes.All(s => s.Key != supplier))
            {
                dbAccommodation.SupplierAccommodationCodes.Add(supplier, supplierAccommodationId);
                dbAccommodation.IsCalculated = false;
                _context.Update(dbAccommodation);
                await _context.SaveChangesAsync(cancellationToken);

                _context.Entry(dbAccommodation).State = EntityState.Detached;
                //TODO get calculated and update here 
                await _accommodationService.RecalculateData(htId);
            }

            return htId;
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


        private readonly IAccommodationService _accommodationService;
        private readonly ILogger<AccommodationMapper> _logger;
        private readonly IAccommodationsTreesCache _treesCache;
        private static List<string> _countries = new List<string>(0);
        private readonly NakijinContext _context;
    }
}