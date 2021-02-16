using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HappyTravel.StaticDataMapper.Api.Models.LocationServiceInfo;
using HappyTravel.StaticDataMapper.Data;
using HappyTravel.StaticDataMapper.Data.Models;
using LocationNameNormalizer.Models;
using Microsoft.EntityFrameworkCore;
using Contracts = HappyTravel.EdoContracts.StaticData;

namespace HappyTravel.StaticDataMapper.Api.Services
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


        public async Task<List<Contracts.Country>> GetCountries(string languageCode, List<Suppliers>? suppliersFilter = null)
        {
            var suppliersKeys = suppliersFilter?.Select(s => s.ToString().ToLower()).ToArray();
            var countriesTable = _context.Countries.Where(c => c.IsActive);
            var localitiesTable = _context.Localities.Where(l => l.IsActive);

            if (suppliersKeys is not null)
            {
                countriesTable = countriesTable.Where(c => EF.Functions.JsonExistAny(c.SupplierCountryCodes, suppliersKeys));
                localitiesTable = localitiesTable.Where(l => EF.Functions.JsonExistAny(l.SupplierLocalityCodes, suppliersKeys));
            }
            
            var countries = await (from c in countriesTable
                join l in localitiesTable on c.Id equals l.CountryId
                select new
                {
                    CountryId = c.Id,
                    CountryCode = c.Code,
                    CountryNames = c.Names,
                    LocalityId = l.Id,
                    LocalityNams = l.Names
                }).ToListAsync();

            return (from c in countries
                group c by new {c.CountryId, c.CountryCode, c.CountryNames}
                into gr
                select new
                    Contracts.Country(gr.Key.CountryCode, HtId.Create(AccommodationMapperLocationTypes.Country, gr.Key.CountryId),
                        gr.Key.CountryNames.GetValueOrDefault(languageCode),
                        gr.Select(l => new Contracts.Locality(HtId.Create(AccommodationMapperLocationTypes.Locality, l.LocalityId),
                            l.LocalityNams.GetValueOrDefault(languageCode))).ToList())).ToList();
        }


        private readonly NakijinContext _context;
    }
}