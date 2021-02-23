using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace HappyTravel.StaticDataMapper.Api.Infrastructure
{
    public static class StringComparisonHelper
    {
        public static float GetEqualityCoefficient(string first, string second, List<string> wordsToIgnore)
        {
            if (string.IsNullOrWhiteSpace(first) || string.IsNullOrWhiteSpace(second))
                return 0;

            return GetJaccardIndex(first.ToSequence(wordsToIgnore), second.ToSequence(wordsToIgnore));
        }

        // For current data was chosen this, but after testing with real data may be changed
        private static float GetJaccardIndex(string[] firstSequence, string[] secondSequence)
        {
            if (!firstSequence.Any() && !secondSequence.Any())
                return 1;

            var intersectedSequence = firstSequence.Intersect(secondSequence).ToArray();
            return (float) intersectedSequence.Length /
                (firstSequence.Length + secondSequence.Length - intersectedSequence.Length);
        }

        private static string[] ToSequence(this string value, List<string> wordsToIgnore)
            => value
                .ToStringWithoutSpecialCharacters()
                .ToStringWithoutMultipleWhitespaces()
                .ToStringWithoutWordsToIgnore(wordsToIgnore)
                .ToStringWithoutMultipleWhitespaces()
                .Split(" ");

        private static string ToStringWithoutWordsToIgnore(this string value, List<string> wordsToIgnore)
        {
            var result = value.ToLowerInvariant();
            foreach (var wordToIgnore in wordsToIgnore)
                    result = result.Replace(wordToIgnore, "");

            return result;
        }

        private static string ToStringWithoutSpecialCharacters(this string value)
            => Regex.Replace(value, SpecialCharactersProcessingPattern, " ",
                RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private static string ToStringWithoutMultipleWhitespaces(this string value)
            => Regex.Replace(value, MultipleSpacesProcessingPattern, " ",
                RegexOptions.Compiled | RegexOptions.CultureInvariant);


        private const string SpecialCharactersProcessingPattern = @"[^\p{L}0-9]+";
        private const string MultipleSpacesProcessingPattern = @"\s+";
    }
}