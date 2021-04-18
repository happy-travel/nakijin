using System;
using System.Collections.Generic;

namespace HappyTravel.Nakijin.Api.Infrastructure.Helpers
{
    public static class ListHelper
    {
        public static IEnumerable<List<T>> Split<T>(List<T> items, int batchSize)
        {
            for (var i = 0; i < items.Count; i += batchSize)
                yield return items.GetRange(i, Math.Min(batchSize, items.Count - i));
        }
    }
}