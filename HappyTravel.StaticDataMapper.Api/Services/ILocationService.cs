using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Contracts = HappyTravel.EdoContracts.StaticData;

namespace HappyTravel.StaticDataMapper.Api.Services
{
    public interface ILocationService
    {
        Task<List<Contracts.Country>> GetCountries(string languageCode);
        Task<DateTime> GetLastModifiedDate();
    }
}