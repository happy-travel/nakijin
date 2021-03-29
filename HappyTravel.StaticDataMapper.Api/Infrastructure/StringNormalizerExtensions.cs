using System;
using System.Text.RegularExpressions;

namespace HappyTravel.Nakijin.Api.Infrastructure
{
    public static class StringNormalizerExtensions
    {
        public static string ToNormalizedPhoneNumber(this string value)
            => Regex.Replace(value, @"[^\d]", "")
                .TrimStart('0');
    }
}