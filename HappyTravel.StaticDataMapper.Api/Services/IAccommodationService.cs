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
        Task<Result<Accommodation>> Get(int accommodationId, string languageCode);
        Task<List<Accommodation>> Get(int skip, int top, string languageCode);
        Task<DateTime> GetLastModifiedDate();
    }
}