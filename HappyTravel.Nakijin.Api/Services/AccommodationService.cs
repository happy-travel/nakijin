using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.MapperContracts.Internal.Mappings.Enums;
using HappyTravel.Nakijin.Api.Infrastructure;
using HappyTravel.Nakijin.Api.Models.LocationServiceInfo;
using HappyTravel.Nakijin.Data;
using HappyTravel.Nakijin.Data.Models.Accommodations;
using HappyTravel.SuppliersCatalog;
using Microsoft.EntityFrameworkCore;
using HappyTravel.MapperContracts.Public.Accommodations;
using HappyTravel.Nakijin.Api.Converters;

namespace HappyTravel.Nakijin.Api.Services
{
    public class AccommodationService : IAccommodationService
    {
        public AccommodationService(NakijinContext context, AccommodationMappingsCache mappingsCache)
        {
            _mappingsCache = mappingsCache;
            _context = context;
        }


        public async Task<Result<Accommodation>> Get(Suppliers supplier, string supplierAccommodationCode,
            string languageCode)
        {
            var searchJson = "{" + $"\"{supplier.ToString().FirstCharToLower()}\":\"{supplierAccommodationCode}\"" + "}";
            var accommodation = await _context.Accommodations
                .Where(ac => ac.IsActive && EF.Functions.JsonContains(ac.SupplierAccommodationCodes, searchJson))
                .Select(ac => new
                {
                    Id = ac.Id,
                    CountryId = ac.CountryId,
                    LocalityId = ac.LocalityId,
                    LocalityZoneId = ac.LocalityZoneId,
                    Data = ac.CalculatedAccommodation,
                    Modified = ac.Modified
                })
                .SingleOrDefaultAsync();

            if (accommodation == null)
                return Result.Failure<Accommodation>("Accommodation does not exists");

            return AccommodationConverter.Convert(accommodation.Id, accommodation.CountryId,
                accommodation.LocalityId, accommodation.LocalityZoneId,
                accommodation.Data, languageCode, accommodation.Modified);
        }


        public async Task<Result<Accommodation>> Get(string htId, string languageCode)
        {
            var (_, isFailure, actualHtId, error) = await GetActualAccommodationHtId(htId);
            if (isFailure)
                return Result.Failure<Accommodation>(error);

            var accommodations = await GetRichDetails(new List<int>(actualHtId));
            var accommodation = accommodations.SingleOrDefault(a => a.Id == actualHtId);

            if (accommodation == default)
                return Result.Failure<Accommodation>("Accommodation does not exists");


            return AccommodationConverter.Convert(accommodation.Id, accommodation.CountryId,
                accommodation.LocalityId, accommodation.LocalityZoneId, accommodation.CalculatedAccommodation,
                languageCode,
                accommodation.Modified);
        }


        public async Task<List<SlimAccommodation>> Get(List<string> htIds, string languageCode)
        {
            var ids = new List<int>();
            foreach (var htId in htIds)
            {
                var (isSuccess, _, id, _) = await GetActualAccommodationHtId(htId);
                if (isSuccess)
                    ids.Add(id);
            }

            return (await GetRichDetails(ids))
                .Select(a => AccommodationConverter.ConvertToSlim(htId: a.Id, 
                    htCountryId: a.CountryId,
                    htLocalityId: a.LocalityId, 
                    htLocalityZoneId: a.LocalityZoneId, 
                    accommodation: a.CalculatedAccommodation,
                    language: languageCode))
                .ToList();
        }


        public Task<DateTime> GetLastModifiedDate() => _context.Accommodations.OrderByDescending(d => d.Modified).Select(l => l.Modified).FirstOrDefaultAsync();


        public async Task<List<Accommodation>> Get(int skip, int top, IEnumerable<Suppliers> suppliersFilter,
            bool? hasDirectContractFilter, string languageCode)
        {
            var suppliersKeys = suppliersFilter.Select(s => s.ToString().FirstCharToLower()).ToArray();
            var accommodationsQuery = _context.Accommodations
                .Where(ac => ac.IsActive);

            if (suppliersKeys.Any())
            {
                accommodationsQuery = accommodationsQuery.Where(ac
                    => EF.Functions.JsonExistAny(ac.SupplierAccommodationCodes, suppliersKeys));
            }

            if (hasDirectContractFilter != null)
            {
                accommodationsQuery =
                    accommodationsQuery.Where(ac => ac.HasDirectContract == hasDirectContractFilter.Value);
            }

            accommodationsQuery = accommodationsQuery
                .OrderBy(ac => ac.Id)
                .Skip(skip)
                .Take(top);

            var accommodations = await accommodationsQuery
                .Select(ac => new
                {
                    Id = ac.Id,
                    CountryId = ac.CountryId,
                    LocalityId = ac.LocalityId,
                    LocalityZoneId = ac.LocalityZoneId,
                    Data = ac.CalculatedAccommodation,
                    ModifiedDate = ac.Modified
                })
                .ToListAsync();

            return accommodations.Select(ac
                    => AccommodationConverter.Convert(ac.Id, ac.CountryId, ac.LocalityId, ac.LocalityZoneId, ac.Data,
                        languageCode, ac.ModifiedDate))
                .ToList();
        }

        
        private Task<List<RichAccommodationDetails>> GetRichDetails(ICollection<int> ids)
            => _context.Accommodations
                .Where(ac => ac.IsActive && ids.Contains(ac.Id))
                .Select(ac => new RichAccommodationDetails
                {
                    Id = ac.Id,
                    CountryId = ac.CountryId,
                    LocalityId = ac.LocalityId,
                    LocalityZoneId = ac.LocalityZoneId,
                    CalculatedAccommodation = ac.CalculatedAccommodation,
                    Modified = ac.Modified
                })
                .ToListAsync();


        private async Task<Result<int>> GetActualAccommodationHtId(string htId)
        {
            var (_, isFailure, (type, id), error) = HtId.Parse(htId);
            if (isFailure)
                return Result.Failure<int>(error);

            if (type != MapperLocationTypes.Accommodation)
                return Result.Failure<int>($"{type} is not supported");

            return await _mappingsCache.GetActualHtId(id);
        }
        

        private readonly AccommodationMappingsCache _mappingsCache;
        private readonly NakijinContext _context;
    }
}