using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using FloxDc.CacheFlow;
using HappyTravel.EdoContracts.Accommodations.Internals;
using HappyTravel.PropertyManagement.Api.Infrastructure;
using HappyTravel.PropertyManagement.Api.Models.Mappers.Enums;
using HappyTravel.PropertyManagement.Data;
using HappyTravel.PropertyManagement.Data.Models;
using HappyTravel.PropertyManagement.Data.Models.Accommodations;
using LocationNameNormalizer.Extensions;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using NetTopologySuite.Index.Strtree;
using Newtonsoft.Json;
using AccommodationDetails = HappyTravel.EdoContracts.Accommodations.AccommodationDetails;
using ContactInfo = HappyTravel.PropertyManagement.Data.Models.Accommodations.ContactInfo;

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
        public AccommodationMapper(NakijinContext context, IAccommodationsTreesCache treesCache)
        {
            _context = context;
            _treesCache = treesCache;
        }

        public async Task MapSupplierAccommodations(Suppliers supplier)
        {
            await ConstructCountryAccommodationsTrees();

            foreach (var countryCode in await GetCountries())
            {
                var accommodationDetails = await GetAccommodationsForMap(countryCode, supplier);
                await MapCountry(accommodationDetails, supplier);
            }
        }

        // TODO: Change and use return value
        private async Task<Dictionary<string, int>> MapCountry(List<AccommodationDetails> accommodations,
            Suppliers supplier)
        {
            var results = new Dictionary<string, int>();

            foreach (var accommodation in accommodations)
            {
                var htId = await Map(accommodation, supplier);
                results.Add(accommodation.Id, htId);
            }

            return results;
        }

        private async Task<int> Map(AccommodationDetails accommodation, Suppliers supplier)
        {
            var normalized = Normalize(accommodation);

            var nearestAccommodations = await GetNearest(normalized);
            if (!nearestAccommodations.Any())
                return await Add(normalized, supplier);

            var (matchingResult, score, htId) = Match(nearestAccommodations, normalized);

            return matchingResult switch
            {
                MatchingResults.NotMatch => await Add(normalized, supplier),
                MatchingResults.Uncertain => await AddUncertainMatches(normalized, supplier, htId, score),
                MatchingResults.Match => await Update(htId, normalized.Id, supplier),
                _ => throw new ArgumentOutOfRangeException(nameof(matchingResult))
            };
        }


        private async Task<List<AccommodationDetails>> GetAccommodationsForMap(string countryCode, Suppliers supplier)
        {
            var accommodations = await (from ac in _context.RawAccommodations
                where ac.Supplier == supplier
                    && ac.CountryCode == countryCode
                select ac).ToListAsync();

            return accommodations.Select(ac
                    => JsonConvert.DeserializeObject<AccommodationDetails>(ac.Accommodation.RootElement.ToString()))
                .ToList();
        }


        private async Task<List<Accommodation>> GetNearest(AccommodationDetails accommodation)
        {
            var tree = await _treesCache.Get(accommodation.Location.CountryCode);

            if (tree == default)
                return new List<Accommodation>();

            var envelope = new Envelope(accommodation.Location.Coordinates.Longitude - 0.01,
                accommodation.Location.Coordinates.Longitude + 0.01,
                accommodation.Location.Coordinates.Latitude - 0.01, accommodation.Location.Coordinates.Latitude + 0.01);
            return tree.Query(envelope).ToList();
        }


        private (MatchingResults results, float score, int htId) Match(List<Accommodation> nearestAccommodations,
            in AccommodationDetails accommodation)
        {
            var results = new List<(int HtId, float Score)>(nearestAccommodations.Count);
            foreach (var nearestAccommodation in nearestAccommodations)
            {
                var score = Score(nearestAccommodation, accommodation);

                results.Add((nearestAccommodation.Id, score));
            }

            var (htId, maxScore) = results.Aggregate((r1, r2) => r2.Score > r1.Score ? r2 : r1);

            if (3 <= maxScore)
                return (MatchingResults.Match, maxScore, htId);

            if (1.5 <= maxScore && maxScore < 3)
                return (MatchingResults.Uncertain, maxScore, htId);

            return (MatchingResults.NotMatch, maxScore, 0);
        }


        private AccommodationDetails Normalize(in AccommodationDetails accommodation)
            => new AccommodationDetails
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
                    accommodation.Location.Directions
                ),
                accommodation.Pictures,
                accommodation.Rating,
                accommodation.RoomAmenities,
                accommodation.Schedule,
                accommodation.TextualDescriptions,
                accommodation.Type,
                accommodation.TypeDescription
            );


        private float Score(in Accommodation nearestAccommodation,
            in AccommodationDetails accommodation)
        {
            float score = 0;

            score += 2 * StringComparisionAlgorithms.GetEqualityCoefficient(nearestAccommodation.Name,
                accommodation.Name,
                new[] {"hotel", "apartments"});

            score += 0.5f * StringComparisionAlgorithms.GetEqualityCoefficient(nearestAccommodation.Address,
                accommodation.Location.Address,
                new[]
                {
                    "street", "area", "road",
                    accommodation.Location.Country.ToLower(CultureInfo.InvariantCulture),
                    accommodation.Location.Locality.ToLower(CultureInfo.InvariantCulture),
                    accommodation.Location.LocalityZone.ToLower(CultureInfo.InvariantCulture)
                });

            if (nearestAccommodation.Rating == accommodation.Rating)
                score += 0.5f;

            var contactInfoComparisionResults = new List<(bool isAnyEmpty, bool areEqual)>(4);

            contactInfoComparisionResults.Add(GetComparisionResult(nearestAccommodation.ContactInfo.Email,
                accommodation.Contacts.Email));
            contactInfoComparisionResults.Add(GetComparisionResult(nearestAccommodation.ContactInfo.WebSite,
                accommodation.Contacts.WebSite));
            contactInfoComparisionResults.Add(GetComparisionResult(nearestAccommodation.ContactInfo.Fax,
                accommodation.Contacts.Fax));
            contactInfoComparisionResults.Add(GetComparisionResult(
                nearestAccommodation.ContactInfo.Phone.ToNormalizedPhoneNumber(),
                accommodation.Contacts.Phone.ToNormalizedPhoneNumber()));

            if (contactInfoComparisionResults.Any(c => !c.isAnyEmpty && c.areEqual) &&
                !contactInfoComparisionResults.Any(c => !c.isAnyEmpty && !c.areEqual))
                score += 0.5f;

            return score;


            static (bool isAnyEmpty, bool areEqual) GetComparisionResult(string first, string second)
            {
                if (string.IsNullOrEmpty(first) || string.IsNullOrEmpty(second))
                    return (true, false);

                var areEqual = first.Trim().ToLower(CultureInfo.InvariantCulture) ==
                    second.Trim().ToLower(CultureInfo.InvariantCulture);
                return (false, areEqual);
            }
        }

        private async Task ConstructCountryAccommodationsTrees()
        {
            foreach (var countryCode in await GetCountries())
            {
                var accommodations =
                    await _context.Accommodations.Where(ac => ac.CountryCode == countryCode).ToListAsync();
                if (!accommodations.Any())
                    continue;

                STRtree<Accommodation> tree = new STRtree<Accommodation>(accommodations.Count);
                foreach (var ac in accommodations)
                {
                    tree.Insert(ac.Coordinates.EnvelopeInternal, ac);
                }

                tree.Build();
                await _treesCache.Set(countryCode, tree);
            }
        }

        private async Task<int> Add(AccommodationDetails accommodation, Suppliers supplier)
        {
            var dbAccommodation = ToDbAccommodation(accommodation);
            dbAccommodation.SupplierAccommodationCodes.Add(supplier, accommodation.Id);
            _context.Accommodations.Add(dbAccommodation);
            await _context.SaveChangesAsync();
            return dbAccommodation.Id;
        }

        private async Task<int> Update(int htId, string supplierAccommodationId, Suppliers supplier)
        {
            var dbAccommodation = await _context.Accommodations.SingleAsync(ac => ac.Id == htId);
            if (dbAccommodation.SupplierAccommodationCodes.All(s => s.Key != supplier))
            {
                dbAccommodation.SupplierAccommodationCodes.Add(supplier, supplierAccommodationId);
                _context.Update(dbAccommodation);
                await _context.SaveChangesAsync();
            }

            return htId;
        }

        private async Task<int> AddUncertainMatches(AccommodationDetails accommodation, Suppliers supplier,
            int existingHtId, float score)
        {
            var newHtId = await Add(accommodation, supplier);
            _context.AccommodationUncertainMatches.Add(new AccommodationUncertainMatches
            {
                Score = score,
                ExistingHtId = existingHtId,
                NewHtId = newHtId,
                IsActive = true
            });

            await _context.SaveChangesAsync();

            return newHtId;
        }

        private static Accommodation ToDbAccommodation(AccommodationDetails accommodationDetails)
            => new Accommodation
            {
                Name = accommodationDetails.Name,
                Address = accommodationDetails.Location.Address,
                Coordinates = new Point(accommodationDetails.Location.Coordinates.Longitude,
                    accommodationDetails.Location.Coordinates.Latitude),
                Rating = accommodationDetails.Rating,
                CountryCode = accommodationDetails.Location.CountryCode,
                ContactInfo = new ContactInfo
                {
                    Email = accommodationDetails.Contacts.Email,
                    Fax = accommodationDetails.Contacts.Fax,
                    Phone = accommodationDetails.Contacts.Phone,
                    WebSite = accommodationDetails.Contacts.WebSite
                },
                AccommodationDetails = JsonDocument.Parse(JsonConvert.SerializeObject(accommodationDetails))
            };

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


        private readonly IAccommodationsTreesCache _treesCache;
        private static List<string> _countries = new List<string>(0);
        private readonly NakijinContext _context;
    }
}