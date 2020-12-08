using System;
using System.Threading.Tasks;
using FloxDc.CacheFlow;
using FloxDc.CacheFlow.Extensions;
using HappyTravel.StaticDataMapper.Data.Models;

namespace HappyTravel.StaticDataMapper.Api.Infrastructure
{
    //Now used only for get localityZoneId during accommodation mapping, maybe later will be used for localityZone Mapping
    public class LocalityZonesCache : ILocalityZonesCache
    {
        public LocalityZonesCache(IDoubleFlow doubleFlow)
        {
            _doubleFlow = doubleFlow;
        }

        public Task Set(string countryCode, string localityName, string localityZoneName, LocalityZone data)
            => _doubleFlow.SetAsync(BuildKey(countryCode, localityName, localityZoneName), data, ExpirationPeriod);

        public ValueTask<LocalityZone?> Get(string countryCode, string localityName, string localityZoneName)
            => _doubleFlow.GetAsync<LocalityZone?>(BuildKey(countryCode, localityName, localityZoneName), ExpirationPeriod);

        private string BuildKey(string countryCode, string localityName, string localityZoneName)
            => _doubleFlow.BuildKey(nameof(LocalityZonesCache), countryCode, localityName, localityZoneName);

        private static readonly TimeSpan ExpirationPeriod = TimeSpan.FromDays(1);

        private readonly IDoubleFlow _doubleFlow;
    }
}