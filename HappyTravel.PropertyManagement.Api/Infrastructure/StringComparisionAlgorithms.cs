using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace HappyTravel.PropertyManagement.Api.Infrastructure
{
    public static class StringComparisionAlgorithms
    {
        public static float GetEqualityCoefficient(string first, string second, string[] unwantedWords)
        {
            if (string.IsNullOrWhiteSpace(first) || string.IsNullOrWhiteSpace(second))
                return 0;
            return GetSorencenDiceCoefficient(first.ToSequence(unwantedWords), second.ToSequence(unwantedWords));
        }

        private static float GetSorencenDiceCoefficient(List<string> firstSequence, List<string> secondSequence)
        {
            //maybe comparision will work in another way
            var intersectedSequence = firstSequence.Intersect(secondSequence).ToArray();
            return 2 * (float) intersectedSequence.Length / (firstSequence.Count() + secondSequence.Count());
        }

        private static List<string> ToSequence(this string value, string[] unWantedWords)
        =>  value.ToStringWithoutSpecialCharacters()
                .ToStringWithoutMultipleWhitespaces()
                .Split(" ")
                .ToList()
                .ToListWithoutUnwantedWords(unWantedWords);

        private static List<string> ToListWithoutUnwantedWords(this List<string> list, string[] unwantedWords)
            => list.Where(str => !unwantedWords.Any(w => w.Contains(str.Trim().ToLower()))).ToList();

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