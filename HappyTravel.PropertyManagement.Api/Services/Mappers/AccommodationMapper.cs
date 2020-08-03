using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HappyTravel.EdoContracts.Accommodations;
using HappyTravel.PropertyManagement.Api.Models.Mappers.Enums;

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
        public AccommodationMapper()
        {
            
        }


        public async Task<int> Map(AccommodationDetails accommodation, List<AccommodationDetails>? accommodationPool = null)
        {
            var normalized = Normalize(accommodation);

            var nearestAccommodations = GetNearest(accommodationPool, normalized);
            if (!nearestAccommodations.Any())
                return Add(normalized);

            var (matchingResult, score, htId) = Match(nearestAccommodations, normalized);

            return matchingResult switch
            {
                MatchingResults.NotMatch => Add(normalized),
                MatchingResults.Uncertain => Update(htId, normalized, score),
                MatchingResults.Match => Update(htId, normalized, score),
                _ => throw new ArgumentOutOfRangeException(nameof(matchingResult))
            };
        }


        public async Task<Dictionary<string, int>> MapCountry(List<AccommodationDetails> accommodations)
        {
            var results = new Dictionary<string, int>();
            var countryPool = new List<AccommodationDetails>();

            foreach (var accommodation in accommodations)
            {
                var htId = await Map(accommodation, countryPool);
                results.Add(accommodation.Id, htId);
            }
            
            return results;
        }


        private int Add(in AccommodationDetails accommodation)
        {
            throw new NotImplementedException();
        }


        private List<AccommodationDetails> GetNearest(List<AccommodationDetails>? accommodationPool, in AccommodationDetails accommodation)
        {
            throw new NotImplementedException();
        }


        private (MatchingResults results, float score, int htId) Match(List<AccommodationDetails> nearestAccommodations, in AccommodationDetails accommodation)
        {
            var results = new List<(int HtId, float Score)>(nearestAccommodations.Count);
            foreach (var nearestAccommodation in nearestAccommodations)
            {
                // skip if accommodation already marked as non-matching

                var id = 0;
                var s = Score(nearestAccommodation, accommodation);
                
                results.Add((id, s));
            }

            var (htId, score) = results.Aggregate((r1, r2) => r2.Score < r1.Score ? r2 : r1);

            if (4.5 <= score)
                return (MatchingResults.Match, score, htId);

            if (2 <= score && score < 4.5)
                return (MatchingResults.Uncertain, score, htId);

            return (MatchingResults.NotMatch, score, 0);
        }


        private AccommodationDetails Normalize(in AccommodationDetails accommodation)
        {
            throw new NotImplementedException();
        }


        private float Score(in AccommodationDetails nearestAccommodations, in AccommodationDetails accommodation)
        {
            throw new NotImplementedException();
        }


        private int Update(in int htId, in AccommodationDetails normalized, in float score)
        {
            throw new NotImplementedException();
        }
    }
}
