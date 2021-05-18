using System.Collections.Generic;
using System.Linq;
using HappyTravel.Nakijin.Api.Models.Mappers;
using HappyTravel.Nakijin.Api.Models.Mappers.Enums;
using NetTopologySuite.Geometries;
using NetTopologySuite.Index.Strtree;
using Contracts = HappyTravel.EdoContracts.Accommodations;

namespace HappyTravel.Nakijin.Api.Services.Workers.AccommodationsMapping
{
    public class AccommodationsMapperHelper
    {
        public AccommodationsMapperHelper(MultilingualDataHelper multilingualDataHelper)
        {
            _multilingualDataHelper = multilingualDataHelper;
        }


        public List<SlimAccommodationData> GetNearest(Contracts.MultilingualAccommodation accommodation,
            STRtree<SlimAccommodationData> tree)
        {
            var accommodationEnvelope = new Envelope(accommodation.Location.Coordinates.Longitude - 0.01,
                accommodation.Location.Coordinates.Longitude + 0.01,
                accommodation.Location.Coordinates.Latitude - 0.01, accommodation.Location.Coordinates.Latitude + 0.01);
            
            return tree.Query(accommodationEnvelope).ToList();
        }


        public (MatchingResults results, float score, SlimAccommodationData slimData) Match(
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


        private readonly MultilingualDataHelper _multilingualDataHelper;

        private const float UncertainMatchingMinimumScore = 1.5f;
        private const float MatchingMinimumScore = 3f;
    }
}