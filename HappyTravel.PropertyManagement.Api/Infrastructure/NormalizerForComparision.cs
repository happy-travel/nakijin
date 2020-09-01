using System;
using System.Text.RegularExpressions;

namespace HappyTravel.PropertyManagement.Api.Infrastructure
{
    public static class NormalizerForComparision
    {
        public static string ToNormalizedPhoneNumber(this string value)
            => Regex.Replace(value, @"[^\d]", "")
                .TrimStart('0');
    }
}