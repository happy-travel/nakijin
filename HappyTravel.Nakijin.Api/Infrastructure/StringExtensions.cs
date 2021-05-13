using System.Linq;

namespace HappyTravel.Nakijin.Api.Infrastructure
{
    public static class StringExtensions
    {
        public static bool IsValid(this string value) => !string.IsNullOrEmpty(value) && value.Any(char.IsLetter);
        
        public static string FirstCharToLower(this string value) => char.ToLower(value[0]) + value[1..];
    }
}