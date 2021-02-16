using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.EdoContracts.Accommodations;
using HappyTravel.StaticDataMapper.Data.Models;

namespace HappyTravel.StaticDataMapper.Api.Services
{
    public interface IAccommodationService
    {
        Task<Result<Accommodation>> Get(Suppliers supplier, string supplierAccommodationCode, string languageCode);
        Task<Result<Accommodation>> Get(string htId, string languageCode, IEnumerable<Suppliers> suppliersFilter);
        Task<List<Accommodation>> Get(int skip, int top, string languageCode, IEnumerable<Suppliers> suppliersFilter);
        Task<DateTime> GetLastModifiedDate();
    }
}