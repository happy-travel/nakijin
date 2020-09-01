using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using HappyTravel.EdoContracts.Accommodations;
using HappyTravel.PropertyManagement.Api.Infrastructure;
using HappyTravel.PropertyManagement.Data.Models.Accommodations;
using Contracts = HappyTravel.EdoContracts.Accommodations.Internals;

namespace HappyTravel.PropertyManagement.Api.Services.Mappers
{
    public static class ComparisionScoreCalculator
    {
        public static float Calculate(in Accommodation nearestAccommodation,
            in AccommodationDetails accommodation)
        {
            float score = 0;

            score += 2 * StringComparisionAlgorithms.GetEqualityCoefficient(nearestAccommodation.Name,
                accommodation.Name, WordsToIgnoreForHotelNamesComparision.ToList());


            score += GetAddressScore(nearestAccommodation, accommodation);

            if (nearestAccommodation.Rating == accommodation.Rating)
                score += 0.5f;

            score += GetContactInfoScore(nearestAccommodation.ContactInfo, accommodation.Contacts);

            return score;
        }

        private static float GetAddressScore(in Accommodation nearestAccommodation,
            in AccommodationDetails accommodation)
        {
            return 0.5f * StringComparisionAlgorithms.GetEqualityCoefficient(nearestAccommodation.Address,
                accommodation.Location.Address, GetWordsToIgnore(accommodation.Location.Country,
                    accommodation.Location.Locality, accommodation.Location.Locality)
            );


            static List<string> GetWordsToIgnore(params string[] wordsToIgnore)
            {
                var wordsToIgnoreForAddressComparision =
                    new List<string>(WordsToIgnoreForAddressesComparision.Length +
                        wordsToIgnore.Length);

                foreach (var word in WordsToIgnoreForAddressesComparision)
                    wordsToIgnoreForAddressComparision.Add(word);

                foreach (var word in wordsToIgnore)
                    if (word != default)
                        wordsToIgnoreForAddressComparision.Add(word.ToLower(CultureInfo.InvariantCulture));

                return wordsToIgnoreForAddressComparision;
            }
        }

        private static float GetContactInfoScore(in ContactInfo nearestAccommodationContactInfo,
            in Contracts.ContactInfo accommodationContactInfo)
        {
            var contactInfoComparisionResults = new List<(bool isAnyEmpty, bool areEqual)>
            {
                GetComparisionResult(nearestAccommodationContactInfo.Email, accommodationContactInfo.Email),
                GetComparisionResult(nearestAccommodationContactInfo.WebSite, accommodationContactInfo.WebSite),
                GetComparisionResult(nearestAccommodationContactInfo.Fax, accommodationContactInfo.Fax),
                GetComparisionResult(nearestAccommodationContactInfo.Phone.ToNormalizedPhoneNumber(),
                    accommodationContactInfo.Phone.ToNormalizedPhoneNumber())
            };

            if (contactInfoComparisionResults.Any(c => !c.isAnyEmpty && c.areEqual) &&
                !contactInfoComparisionResults.Any(c => !c.isAnyEmpty && !c.areEqual))
                return 0.5f;

            return 0;


            static (bool isAnyEmpty, bool areEqual) GetComparisionResult(string first, string second)
            {
                if (string.IsNullOrEmpty(first) || string.IsNullOrEmpty(second))
                    return (true, false);

                var areEqual = first.Trim().ToLower(CultureInfo.InvariantCulture) ==
                    second.Trim().ToLower(CultureInfo.InvariantCulture);
                return (false, areEqual);
            }
        }

        public static readonly string[] WordsToIgnoreForHotelNamesComparision = new[] {"hotel", "apartments"};
        public static readonly string[] WordsToIgnoreForAddressesComparision = new[] {"street", "area", "road",};
    }
}