using System;
using System.Threading.Tasks;
using FloxDc.CacheFlow;
using FloxDc.CacheFlow.Extensions;
using HappyTravel.StaticDataMapper.Api.Models.Mappers;
using NetTopologySuite.Index.Strtree;

namespace HappyTravel.StaticDataMapper.Api.Infrastructure
{
    public class AccommodationsTreesCache : IAccommodationsTreesCache
    {
        public AccommodationsTreesCache(IDoubleFlow doubleFlow)
        {
            _doubleFlow = doubleFlow;
        }

        public Task Set(string countryCode, STRtree<AccommodationKeyData> tree)
            => _doubleFlow.SetAsync(BuildKey(countryCode), tree, ExpirationPeriod);

        public ValueTask<STRtree<AccommodationKeyData>> Get(string countryCode)
            => _doubleFlow.GetAsync<STRtree<AccommodationKeyData>>(BuildKey(countryCode), ExpirationPeriod);

        private string BuildKey(string countryCode)
            => _doubleFlow.BuildKey(nameof(AccommodationsTreesCache), countryCode);

        private static readonly TimeSpan ExpirationPeriod = TimeSpan.FromDays(1);

        private readonly IDoubleFlow _doubleFlow;
    }
}