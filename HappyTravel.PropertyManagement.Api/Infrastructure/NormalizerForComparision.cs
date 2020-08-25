using Microsoft.EntityFrameworkCore.Storage;

namespace HappyTravel.PropertyManagement.Api.Infrastructure
{
    public static class NormalizerForComparision
    {
        public static string ToNormalizedPhoneNumber(this string value)
            => value.Trim()
                .TrimStart('0')
                .Replace("-",string.Empty)
                .Replace("+",string.Empty);
    }
}