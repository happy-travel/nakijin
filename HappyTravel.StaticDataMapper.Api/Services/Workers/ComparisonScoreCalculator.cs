using System.Collections.Generic;
using System.Linq;
using HappyTravel.EdoContracts.Accommodations;
using HappyTravel.StaticDataMapper.Api.Infrastructure;
using Contracts = HappyTravel.EdoContracts.Accommodations.Internals;

namespace HappyTravel.StaticDataMapper.Api.Services.Workers
{
    public static class ComparisonScoreCalculator
    {
        // Considering that accommodations always have default(En) value
        public static float Calculate(in MultilingualAccommodation nearestAccommodation,
            in MultilingualAccommodation accommodation)
        {
            float score = 2 * StringComparisonHelper.GetEqualityCoefficient(nearestAccommodation.Name.En,
                accommodation.Name.En, WordsToIgnoreForHotelNamesComparison);

            score += GetAddressScore(nearestAccommodation, accommodation);

            if (nearestAccommodation.Rating == accommodation.Rating)
                score += 0.5f;

            score += GetContactInfoScore(nearestAccommodation.Contacts, accommodation.Contacts);

            return score;
        }


        private static float GetAddressScore(in MultilingualAccommodation nearestAccommodation,
            in MultilingualAccommodation accommodation)
        {
            return 0.5f * StringComparisonHelper.GetEqualityCoefficient(nearestAccommodation.Location.Address.En,
                accommodation.Location.Address.En, GetWordsToIgnore(accommodation.Location.Country.En,
                    //Not all providers have localityZone
                    accommodation.Location.Locality?.En, accommodation.Location.LocalityZone?.En,
                    nearestAccommodation.Location.LocalityZone?.En)
            );


            static List<string> GetWordsToIgnore(params string[] wordsToIgnore)
            {
                var wordsToIgnoreForAddressComparison =
                    new List<string>(WordsToIgnoreForAddressesComparison);

                foreach (var word in wordsToIgnore)
                    if (word != default)
                        wordsToIgnoreForAddressComparison.Add(word.ToLowerInvariant());

                return wordsToIgnoreForAddressComparison;
            }
        }


        private static float GetContactInfoScore(in Contracts.ContactInfo nearestAccommodationContactInfo,
            in Contracts.ContactInfo accommodationContactInfo)
        {
            var contactInfoComparisonResults = new List<(bool isAnyEmpty, bool areContains)>
            {
                GetComparisonResult(nearestAccommodationContactInfo.Emails, accommodationContactInfo.Emails),
                GetComparisonResult(nearestAccommodationContactInfo.WebSites, accommodationContactInfo.WebSites),
                GetComparisonResult(nearestAccommodationContactInfo.Faxes, accommodationContactInfo.Faxes),
                GetComparisonResult(
                    nearestAccommodationContactInfo.Phones.Select(ph => ph?.ToNormalizedPhoneNumber()).ToList(),
                    accommodationContactInfo.Phones.Select(p => p?.ToNormalizedPhoneNumber()).ToList())
            };

            if (contactInfoComparisonResults.Any(c => !c.isAnyEmpty && c.areContains) &&
                !contactInfoComparisonResults.Any(c => !c.isAnyEmpty && !c.areContains))
                return 0.5f;

            return 0;


            static (bool isAnyEmpty, bool areContains) GetComparisonResult(List<string> first, List<string> second)
            {
                if (!first.Any() || !second.Any() || first.All(string.IsNullOrEmpty) ||
                    second.All(string.IsNullOrEmpty))
                    return (true, false);

                var mergedData = (from f in first
                    join s in second
                        on f?.Trim().ToLowerInvariant() equals s?.Trim().ToLowerInvariant()
                    select f);

                return (false, mergedData.Any());
            }
        }


        public static readonly List<string> WordsToIgnoreForHotelNamesComparison =
            new List<string> {"hotel", "apartments", "apartment"};

        public static readonly List<string> WordsToIgnoreForAddressesComparison =
            new List<string> {"street", "area", "road",};
    }
}