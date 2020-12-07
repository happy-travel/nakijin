using System;
using System.Threading.Tasks;
using FloxDc.CacheFlow;
using FloxDc.CacheFlow.Extensions;
using HappyTravel.StaticDataMapper.Data.Models;

namespace HappyTravel.StaticDataMapper.Api.Infrastructure
{
    public class CountriesCache : ICountriesCache
    {
        public CountriesCache(IDoubleFlow doubleFlow)
        {
            _doubleFlow = doubleFlow;
        }

        public Task Set(string countryCode, Country data)
            => _doubleFlow.SetAsync(BuildKey(countryCode), data, ExpirationPeriod);

        public ValueTask<Country?> Get(string countryCode)
            => _doubleFlow.GetAsync<Country?>(BuildKey(countryCode), ExpirationPeriod);


        private string BuildKey(string countryCode) => _doubleFlow.BuildKey(nameof(CountriesCache), countryCode);

        private static readonly TimeSpan ExpirationPeriod = TimeSpan.FromDays(1);

        private readonly IDoubleFlow _doubleFlow;
    }
}