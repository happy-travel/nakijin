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

            return GetSorensenDiceCoefficient(first.ToSequence(wordsToIgnore), second.ToSequence(wordsToIgnore));
        }

        private static float GetSorensenDiceCoefficient(string[] firstSequence, string[] secondSequence)
        {
            if (!firstSequence.Any() && !secondSequence.Any())
                return 1;

            // Maybe comparison will work in another way
            var intersectedSequence = firstSequence.Intersect(secondSequence).ToArray();
            return 2 * (float) intersectedSequence.Length / (firstSequence.Length + secondSequence.Length);
        }

        private static string[] ToSequence(this string value, List<string> wordsToIgnore)
            => value.ToStringWithoutSpecialCharacters()
                .ToStringWithoutMultipleWhitespaces()
                .Split(" ")
                .ToArrayWithoutWordsToIgnore(wordsToIgnore);

        private static string[] ToArrayWithoutWordsToIgnore(this string[] arr, List<string> wordsToIgnore)
            => arr.Where(str => !wordsToIgnore.Any(w => w.Contains(str.Trim().ToLowerInvariant()))).ToArray();

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