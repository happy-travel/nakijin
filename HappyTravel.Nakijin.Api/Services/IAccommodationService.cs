using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.MapperContracts.Public.Accommodations;
using HappyTravel.SuppliersCatalog;

namespace HappyTravel.Nakijin.Api.Services
{
    public interface IAccommodationService
    {
        Task<Result<Accommodation>> Get(Suppliers supplier, string supplierAccommodationCode, string languageCode);

        Task<Result<Accommodation>> Get(string htId, string languageCode);
        Task<List<SlimAccommodation>> Get(List<string> htIds, string languageCode);

        Task<List<Accommodation>> Get(int skip, int top, IEnumerable<Suppliers> suppliersFilter, bool? hasDirectContractFilter, string languageCode);

        Task<DateTime> GetLastModifiedDate();
    }
}