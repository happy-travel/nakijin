using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HappyTravel.StaticDataMapper.Api.Models.LocationServiceInfo;
using Contracts = HappyTravel.EdoContracts.StaticData;

namespace HappyTravel.StaticDataMapper.Api.Services
{
    public interface ILocationService
    {
        Task<List<Contracts.Country>> GetCountries(string languageCode);

        Task<List<Location>> Get(AccommodationMapperLocationTypes locationType, string languageCode,
            DateTime modified, int skip = 0, int top = 50000, CancellationToken cancellationToken = default);
    }
}