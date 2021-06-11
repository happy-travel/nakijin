using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HappyTravel.MapperContracts.Public.Locations;
using HappyTravel.SuppliersCatalog;

namespace HappyTravel.Nakijin.Api.Services
{
    public interface ILocationService
    {
        Task<List<Country>> GetCountries(IEnumerable<Suppliers> suppliersFilter, string languageCode);

        Task<DateTime> GetLastModifiedDate();
    }
}