using System;
using System.Threading.Tasks;
using FloxDc.CacheFlow;
using FloxDc.CacheFlow.Extensions;
using HappyTravel.StaticDataMapper.Data.Models;

namespace HappyTravel.StaticDataMapper.Api.Infrastructure
{
    public class LocalitiesCache : ILocalitiesCache
    {
        public LocalitiesCache(IDoubleFlow doubleFlow)
        {
            _doubleFlow = doubleFlow;
        }

        public Task Set(string countryCode, string localityName, Locality data)
            => _doubleFlow.SetAsync(BuildKey(countryCode, localityName), data, ExpirationPeriod);

        public ValueTask<Locality?> Get(string countryCode, string localityName)
            => _doubleFlow.GetAsync<Locality?>(BuildKey(countryCode, localityName), ExpirationPeriod);


        private string BuildKey(string countryCode, string localityName)
            => _doubleFlow.BuildKey(nameof(LocalitiesCache), countryCode, localityName);

        private static readonly TimeSpan ExpirationPeriod = TimeSpan.FromDays(1);

        private readonly IDoubleFlow _doubleFlow;
    }
}