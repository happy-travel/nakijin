using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HappyTravel.StaticDataMapper.Data;
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


        public async Task<List<Contracts.Country>> GetCountries(string languageCode)
        {
            var countries = await (from c in _context.Countries
                join l in _context.Localities on c.Id equals l.CountryId
                where c.IsActive && l.IsActive
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
                    Contracts.Country(gr.Key.CountryCode, gr.Key.CountryId.ToString(),
                        gr.Key.CountryNames.GetValueOrDefault(languageCode),
                        gr.Select(l => new Contracts.Locality(l.LocalityId.ToString(),
                            l.LocalityNams.GetValueOrDefault(languageCode))).ToList())).ToList();
        }


        private readonly NakijinContext _context;
    }
}