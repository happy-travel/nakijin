using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HappyTravel.MapperContracts.Internal.Mappings.Enums;
using HappyTravel.MapperContracts.Public.Locations;
using HappyTravel.Nakijin.Api.Infrastructure;
using HappyTravel.Nakijin.Data;
using HappyTravel.SuppliersCatalog;
using Microsoft.EntityFrameworkCore;

namespace HappyTravel.Nakijin.Api.Services
{
    public class LocationService : ILocationService
    {
        public LocationService(NakijinContext context)
        {
            _context = context;
        }

        public async Task<DateTime> GetLastModifiedDate()
        {
            var countriesLastModifiedDate = await _context.Countries.OrderByDescending(c => c.Modified)
                .Select(c => c.Modified).FirstOrDefaultAsync();

            var localitiesLastModifiedDate = await _context.Localities.OrderByDescending(l => l.Modified)
                .Select(l => l.Modified).FirstOrDefaultAsync();

            // For this api no need to consider locality zones modified date
            return countriesLastModifiedDate > localitiesLastModifiedDate
                ? countriesLastModifiedDate
                : localitiesLastModifiedDate;
        }


        public async Task<List<Country>> GetCountries(IEnumerable<Suppliers> suppliersFilter, string languageCode)
        {
            var suppliersKeys = suppliersFilter.Select(s => s.ToString().FirstCharToLower()).ToArray();
            var countriesQuery = _context.Countries.Where(c => c.IsActive);
            var localitiesQuery = _context.Localities.Where(l => l.IsActive);

            if (suppliersKeys.Any())
            {
                countriesQuery = countriesQuery.Where(c => EF.Functions.JsonExistAny(c.SupplierCountryCodes, suppliersKeys));
                localitiesQuery = localitiesQuery.Where(l => EF.Functions.JsonExistAny(l.SupplierLocalityCodes, suppliersKeys));
            }
            
            var countries = await (from c in countriesQuery
                join l in localitiesQuery on c.Id equals l.CountryId
                select new
                {
                    CountryId = c.Id,
                    CountryCode = c.Code,
                    CountryNames = c.Names,
                    LocalityId = l.Id,
                    LocalityNamеs = l.Names
                }).ToListAsync();

            return (from c in countries
                group c by new {c.CountryId, c.CountryCode, c.CountryNames}
                into gr
                select new
                    Country(gr.Key.CountryCode, HtId.Create(MapperLocationTypes.Country, gr.Key.CountryId),
                        gr.Key.CountryNames.GetValueOrDefault(languageCode),
                        gr.Select(l => new Locality(HtId.Create(MapperLocationTypes.Locality, l.LocalityId),
                            l.LocalityNamеs.GetValueOrDefault(languageCode))).ToList())).ToList();
        }


        private readonly NakijinContext _context;
    }
}