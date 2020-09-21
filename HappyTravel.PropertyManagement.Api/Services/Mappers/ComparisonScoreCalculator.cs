using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using HappyTravel.EdoContracts.Accommodations;
using HappyTravel.PropertyManagement.Api.Infrastructure;
using HappyTravel.PropertyManagement.Data.Models.Accommodations;
using Contracts = HappyTravel.EdoContracts.Accommodations.Internals;

namespace HappyTravel.PropertyManagement.Api.Services.Mappers
{
    public static class ComparisonScoreCalculator
    {
        public static float Calculate(in Accommodation nearestAccommodation,
            in AccommodationDetails accommodation)
        {
            float score = 2 * StringComparisonHelper.GetEqualityCoefficient(nearestAccommodation.Name,
                accommodation.Name, WordsToIgnoreForHotelNamesComparison);

            score += GetAddressScore(nearestAccommodation, accommodation);

            if (nearestAccommodation.Rating == accommodation.Rating)
                score += 0.5f;

            score += GetContactInfoScore(nearestAccommodation.ContactInfo, accommodation.Contacts);

            return score;
        }

        private static float GetAddressScore(in Accommodation nearestAccommodation,
            in AccommodationDetails accommodation)
        {
            return 0.5f * StringComparisonHelper.GetEqualityCoefficient(nearestAccommodation.Address,
                accommodation.Location.Address, GetWordsToIgnore(accommodation.Location.Country,
                    accommodation.Location.Locality, accommodation.Location.Locality)
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

        private static float GetContactInfoScore(in ContactInfo nearestAccommodationContactInfo,
            in Contracts.ContactInfo accommodationContactInfo)
        {
            var contactInfoComparisonResults = new List<(bool isAnyEmpty, bool areEqual)>
            {
                GetComparisonResult(nearestAccommodationContactInfo.Email, accommodationContactInfo.Email),
                GetComparisonResult(nearestAccommodationContactInfo.WebSite, accommodationContactInfo.WebSite),
                GetComparisonResult(nearestAccommodationContactInfo.Fax, accommodationContactInfo.Fax),
                GetComparisonResult(nearestAccommodationContactInfo.Phone.ToNormalizedPhoneNumber(),
                    accommodationContactInfo.Phone.ToNormalizedPhoneNumber())
            };

            if (contactInfoComparisonResults.Any(c => !c.isAnyEmpty && c.areEqual) &&
                !contactInfoComparisonResults.Any(c => !c.isAnyEmpty && !c.areEqual))
                return 0.5f;

            return 0;


            static (bool isAnyEmpty, bool areEqual) GetComparisonResult(string first, string second)
            {
                if (string.IsNullOrEmpty(first) || string.IsNullOrEmpty(second))
                    return (true, false);

                var areEqual = first.Trim().ToLowerInvariant() ==
                    second.Trim().ToLowerInvariant();
                return (false, areEqual);
            }
        }

        public static readonly List<string> WordsToIgnoreForHotelNamesComparison =
            new List<string> {"hotel", "apartments"};

        public static readonly List<string> WordsToIgnoreForAddressesComparison =
            new List<string> {"street", "area", "road",};
    }
}