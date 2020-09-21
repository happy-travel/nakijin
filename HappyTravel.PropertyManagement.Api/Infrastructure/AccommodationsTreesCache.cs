using System;
using System.Threading.Tasks;
using FloxDc.CacheFlow;
using FloxDc.CacheFlow.Extensions;
using HappyTravel.PropertyManagement.Data.Models.Accommodations;
using NetTopologySuite.Index.Strtree;

namespace HappyTravel.PropertyManagement.Api.Infrastructure
{
    public class AccommodationsTreesCache : IAccommodationsTreesCache
    {
        public AccommodationsTreesCache(IDoubleFlow doubleFlow)
        {
            _doubleFlow = doubleFlow;
        }

        public Task Set(string countryCode, STRtree<Accommodation> tree)
            => _doubleFlow.SetAsync(BuildKey(countryCode), tree, ExpirationPeriod);

        public ValueTask<STRtree<Accommodation>> Get(string countryCode)
            => _doubleFlow.GetAsync<STRtree<Accommodation>>(BuildKey(countryCode), ExpirationPeriod);

        private string BuildKey(string countryCode)
            => _doubleFlow.BuildKey(nameof(AccommodationsTreesCache), countryCode);

        private static readonly TimeSpan ExpirationPeriod = TimeSpan.FromDays(1);

        private readonly IDoubleFlow _doubleFlow;
    }
}