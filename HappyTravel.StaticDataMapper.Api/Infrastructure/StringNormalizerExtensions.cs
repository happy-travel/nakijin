using System;
using System.Text.RegularExpressions;

namespace HappyTravel.StaticDataMapper.Api.Infrastructure
{
    public static class StringNormalizerExtensions
    {
        public static string ToNormalizedPhoneNumber(this string value)
            => Regex.Replace(value, @"[^\d]", "")
                .TrimStart('0');
    }
}