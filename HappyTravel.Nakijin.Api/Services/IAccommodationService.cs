using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.EdoContracts.Accommodations;
using HappyTravel.Nakijin.Data.Models;

namespace HappyTravel.Nakijin.Api.Services
{
    public interface IAccommodationService
    {
        Task<Result<Accommodation>> Get(Suppliers supplier, string supplierAccommodationCode, string languageCode);
        Task<Result<Accommodation>> Get(string htId, string languageCode);

        Task<List<Accommodation>> Get(int skip, int top, IEnumerable<Suppliers> suppliersFilter, bool? hasDirectContractFilter,
            string languageCode);

        Task<DateTime> GetLastModifiedDate();
    }
}