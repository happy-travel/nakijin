using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HappyTravel.StaticDataMapper.Data;
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

        public async Task<List<Contracts.Country>> GetCountries(string languageCode)
        {
            var countries = await _context.Countries
                .Where(c => c.IsActive)
                .Select(c => new
                {
                    Code = c.Code,
                    Names = c.Names,
                    Modified = c.Modified
                }).ToListAsync();

            return countries.Select(c
                    => new Contracts.Country(c.Code, c.Names.GetValueOrDefault(languageCode), c.Modified))
                .ToList();
        }

        private readonly NakijinContext _context;
    }
}