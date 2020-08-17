using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
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
            * Partial match of a normalized name — 1 point
            * Full match of a normalized address — 2 points
            * Partial match of a normalized address — 1 point
            * Rating match — 0.5 points
            * Contact details match — 0.5 points
           If an accommodation scores less than 2 points we consider it not-matching.
           If  an accommodation scores greater than 4.5 points we consider it matching.
           Intermediate scores should be calibrated to achieve better matching
        5. If the score is sufficient, merge the new and the existing accommodation. Unmatched field became synonyms
    */
    public class AccommodationMapper
    {
        public AccommodationMapper(NakijinContext context)
        {
            _context = context;
            _accommodationTreesByCountry = new Dictionary<string, STRtree<Accommodation>>();
        }


        public async Task<int> Map(AccommodationDetails accommodation, Suppliers supplier)
        {
            var normalized = Normalize(accommodation);

            var nearestAccommodations = GetNearest(normalized);
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


        public async Task<Dictionary<string, int>> MapCountry(List<AccommodationDetails> accommodations,
            Suppliers supplier)
        {
            var results = new Dictionary<string, int>();

            await ConstructCountryAccommodationsTrees();

            foreach (var accommodation in accommodations)
            {
                var htId = await Map(accommodation, supplier);
                results.Add(accommodation.Id, htId);
            }

            return results;
        }


        private List<Accommodation> GetNearest(in AccommodationDetails accommodation)
        {
            if (!_accommodationTreesByCountry.TryGetValue(accommodation.Location.CountryCode, out var tree))
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
                // skip if accommodation already marked as non-matching

                var s = Score(nearestAccommodation, accommodation);

                results.Add((nearestAccommodation.Id, s));
            }

            var (htId, score) = results.Aggregate((r1, r2) => r2.Score < r1.Score ? r2 : r1);

            if (4.5 <= score)
                return (MatchingResults.Match, score, htId);

            if (2 <= score && score < 4.5)
                return (MatchingResults.Uncertain, score, htId);

            return (MatchingResults.NotMatch, score, 0);
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
                    accommodation.Location.LocalityCode,
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


        private float Score(in Accommodation nearestAccommodation, in AccommodationDetails accommodation)
        {
            float score = 0;

            score += 2 * StringComparisionAlgorithms.GetEqualityIndex(nearestAccommodation.Name, accommodation.Name);

            score += StringComparisionAlgorithms.GetEqualityIndex(nearestAccommodation.Address,
                accommodation.Location.Address);

            if (nearestAccommodation.Rating == accommodation.Rating)
                score += 0.5f;

            if (nearestAccommodation.ContactInfo.Email.Trim().ToLower(CultureInfo.InvariantCulture)
                == accommodation.Contacts.Email.Trim().ToLower(CultureInfo.InvariantCulture)
                && nearestAccommodation.ContactInfo.WebSite.Trim().ToLower(CultureInfo.InvariantCulture) ==
                accommodation.Contacts.WebSite.Trim().ToLower(CultureInfo.InvariantCulture)
                && nearestAccommodation.ContactInfo.Fax.Trim().ToLower(CultureInfo.InvariantCulture) ==
                accommodation.Contacts.Fax.Trim().ToLower(CultureInfo.InvariantCulture)
                && nearestAccommodation.ContactInfo.Phone.Trim().ToLower(CultureInfo.InvariantCulture) ==
                nearestAccommodation.ContactInfo.Fax.Trim().ToLower(CultureInfo.InvariantCulture))
                score += 0.5f;

            return score;
        }

        private async Task ConstructCountryAccommodationsTrees()
        {
            var accommodations = await _context.Accommodations.ToListAsync();
            var groupedAccommodations = accommodations.GroupBy(ac => ac.CountryCode);
            foreach (var group in groupedAccommodations)
            {
                STRtree<Accommodation> tree = new STRtree<Accommodation>(group.Count());
                foreach (var ac in group)
                {
                    tree.Insert(ac.Coordinates.EnvelopeInternal, ac);
                }

                tree.Build();
                _accommodationTreesByCountry.Add(group.Key, tree);
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

        private async Task<int> Update(int htId, string supplierAccommodationId, Suppliers supplier /*in float score*/)
        {
            var dbAccommodation = await _context.Accommodations.SingleAsync(ac => ac.Id == htId);
            dbAccommodation.SupplierAccommodationCodes.Add(supplier, supplierAccommodationId);
            _context.Update(dbAccommodation);
            await _context.SaveChangesAsync();
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
                Coordinates = new Point(accommodationDetails.Location.Coordinates.Latitude,
                    accommodationDetails.Location.Coordinates.Longitude),
                Rating = accommodationDetails.Rating,
                CountryCode = accommodationDetails.Location.CountryCode,
                ContactInfo = new ContactInfo
                {
                    Email = accommodationDetails.Contacts.Email,
                    Fax = accommodationDetails.Contacts.Fax,
                    Phone = accommodationDetails.Contacts.Phone,
                    WebSite = accommodationDetails.Contacts.WebSite
                },
                AccommodationDetails = JsonDocument.Parse(JsonSerializer.Serialize(accommodationDetails))
            };


        private Dictionary<string, STRtree<Accommodation>> _accommodationTreesByCountry;
        private readonly NakijinContext _context;
    }
}