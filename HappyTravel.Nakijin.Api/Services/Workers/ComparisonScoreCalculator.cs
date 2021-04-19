using System;
using System.Collections.Generic;
using System.Linq;
using HappyTravel.EdoContracts.Accommodations;
using HappyTravel.Nakijin.Api.Infrastructure;
using HappyTravel.Nakijin.Data.Models.Accommodations;
using Contracts = HappyTravel.EdoContracts.Accommodations.Internals;

namespace HappyTravel.Nakijin.Api.Services.Workers
{
    public static class ComparisonScoreCalculator
    {
        // Considering that accommodations always have default(En) value
        public static float Calculate(in AccommodationMappingData nearestAccommodation,
            in AccommodationMappingData accommodation)
        {
            float score = NameScore * GetNamesScore(nearestAccommodation, accommodation);

            if (score == NameScore
                && nearestAccommodation.DefaultLocalityName != null
                && accommodation.DefaultLocalityName != null
                && String.Equals(nearestAccommodation.DefaultLocalityName, accommodation.DefaultLocalityName,
                    StringComparison.CurrentCultureIgnoreCase))
                return MaxScore;

            score += GetAddressScore(nearestAccommodation, accommodation);

            if (nearestAccommodation.Rating == accommodation.Rating)
                score += RatingScore;

            score += GetContactInfoScore(nearestAccommodation.ContactInfo, accommodation.ContactInfo);

            return score;
        }


        private static float GetNamesScore(in AccommodationMappingData nearestAccommodation,
            in AccommodationMappingData accommodation)
        {
            var locationsNamesToIgnore =
                GetLocationsNamesForIgnoreOnNameComparision(nearestAccommodation, accommodation);

            if (nearestAccommodation.DefaultName.Contains("hotel", StringComparison.InvariantCultureIgnoreCase) &&
                nearestAccommodation.DefaultName.Contains("apartment", StringComparison.InvariantCultureIgnoreCase)
                || accommodation.DefaultName.Contains("hotel", StringComparison.InvariantCultureIgnoreCase) &&
                accommodation.DefaultName.Contains("apartment", StringComparison.InvariantCultureIgnoreCase))
            {
                return StringComparisonHelper.GetEqualityCoefficient(nearestAccommodation.DefaultName,
                    accommodation.DefaultName,
                    GetWordsToIgnore(locationsNamesToIgnore));
            }

            var scores = new List<float>(WordsToIgnoreSetForHotelNamesComparison.Count + 1);
            foreach (var wordsToIgnore in WordsToIgnoreSetForHotelNamesComparison)
            {
                scores.Add(StringComparisonHelper.GetEqualityCoefficient(nearestAccommodation.DefaultName,
                    accommodation.DefaultName,
                    GetWordsToIgnore(locationsNamesToIgnore, wordsToIgnore)
                ));
            }

            return scores.Max();
        }

        private static List<string> GetLocationsNamesForIgnoreOnNameComparision(
            in AccommodationMappingData nearestAccommodation,
            in AccommodationMappingData accommodation)
        {
            var result = new List<string>();

            result.AddRange(GetWords(nearestAccommodation.DefaultCountryName));
            result.AddRange(GetWords(accommodation.DefaultCountryName));
            result.AddRange(GetWords(accommodation.DefaultLocalityName));
            result.AddRange(GetWords(nearestAccommodation.DefaultLocalityName));
            result.AddRange(GetWords(accommodation.DefaultLocalityZoneName));
            result.AddRange(GetWords(nearestAccommodation.DefaultLocalityZoneName));

            return result.Distinct().ToList();


            List<string> GetWords(string value)
                => string.IsNullOrEmpty(value)
                    ? new List<string>()
                    : value.Split(" ").ToList();
        }

        private static float GetAddressScore(in AccommodationMappingData nearestAccommodation,
            in AccommodationMappingData accommodation)
        {
            return AddressScore * StringComparisonHelper.GetEqualityCoefficient(
                nearestAccommodation.Address,
                accommodation.Address, GetWordsToIgnore(
                    GetLocationsNamesForIgnoreOnNameComparision(nearestAccommodation, accommodation),
                    WordsToIgnoreForAddressesComparison));
        }

        private static List<string> GetWordsToIgnore(List<string> wordsToIgnore, string[]? constantWords = default)
        {
            var result = new List<string>(CommonWordsToIgnore);
            if (constantWords != default)
                result.AddRange(constantWords);

            foreach (var word in wordsToIgnore)
                if (!string.IsNullOrEmpty(word))
                    result.Add(word.ToLowerInvariant());

            return result;
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
                return ContactsScore;

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

        private const float MaxScore = 3.5f;
        private const float NameScore = 2f;
        private const float AddressScore = 0.5f;
        private const float ContactsScore = 0.5f;
        private const float RatingScore = 0.5f;

        private static readonly List<string[]> WordsToIgnoreSetForHotelNamesComparison =
            new List<string[]>
            {
                new string[] {"apartments", "apartment"},
                new string[] {"hotel", "hotels"}
            };


        private static readonly string[] WordsToIgnoreForAddressesComparison =
            new string[] {"street", "area", "road",};

        private static readonly string[] CommonWordsToIgnore = new string[]
        {
            "a", "an", "at", "the", "on"
        };
    }
}