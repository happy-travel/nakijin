using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HappyTravel.StaticDataMapper.Data.Models;
using Contracts = HappyTravel.EdoContracts.StaticData;

namespace HappyTravel.StaticDataMapper.Api.Services
{
    public interface ILocationService
    {
        Task<List<Contracts.Country>> GetCountries(string languageCode, List<Suppliers>? suppliersFilter);
        Task<DateTime> GetLastModifiedDate();
    }
}