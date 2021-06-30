using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace HappyTravel.Nakijin.Api.Infrastructure
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

            var intersectedSequence = firstSequence.Intersect(secondSequence, new StringComparer()).ToArray();
            return (float) intersectedSequence.Length /
                (firstSequence.Length + secondSequence.Length - intersectedSequence.Length);
        }

        private static string[] ToSequence(this string value, List<string> wordsToIgnore)
            => value
                .ToStringWithoutSpecialCharacters()
                .ToStringWithoutMultipleWhitespaces()
                .Split(" ")
                .ToArrayWithoutWordsToIgnore(wordsToIgnore);

        private static string[] ToArrayWithoutWordsToIgnore(this string[] value, List<string> wordsToIgnore)
            => value.Where(str => !wordsToIgnore.Any(w => w.Contains(str.Trim().ToLowerInvariant()))).Distinct()
                .ToArray();

        private static string ToStringWithoutSpecialCharacters(this string value)
            => Regex.Replace(value, SpecialCharactersProcessingPattern, " ",
                RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private static string ToStringWithoutMultipleWhitespaces(this string value)
            => Regex.Replace(value, MultipleSpacesProcessingPattern, " ",
                RegexOptions.Compiled | RegexOptions.CultureInvariant);


        private const string SpecialCharactersProcessingPattern = @"[^\p{L}0-9]+";
        private const string MultipleSpacesProcessingPattern = @"\s+";
    }

    internal class StringComparer : IEqualityComparer<string>
    {
        public bool Equals(string? x, string? y)
        {
            if (string.IsNullOrEmpty(x) || string.IsNullOrEmpty(y))
                return false;

            var lowerX = x.ToLowerInvariant();
            var lowerY = y.ToLowerInvariant();
            // For name comparision we  consider words equal if one contains another: to ignore prefixes and endings.
            return lowerX.Contains(lowerY) || lowerY.Contains(lowerX);
        }

        // Always return the same value to check all pairs with method Equals
        public int GetHashCode(string obj) => 1;
    }
}