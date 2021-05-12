using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HappyTravel.LocationNameNormalizer;
using HappyTravel.Nakijin.Api.Models;
using HappyTravel.Nakijin.Data;
using HappyTravel.Nakijin.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace HappyTravel.Nakijin.Api.Services.Workers.LocationsMapping
{
    public class LocationsMapperDataRetrieveService : ILocationsMapperDataRetrieveService
    {
        public LocationsMapperDataRetrieveService(NakijinContext context, MultilingualDataHelper multilingualDataHelper,
            ILocationNameNormalizer locationNameNormalizer)
        {
            _multilingualDataHelper = multilingualDataHelper;
            _locationNameNormalizer = locationNameNormalizer;
            _context = context;
        }


        // TODO: Maybe will be added normalization of raw data (not final data)
        public async Task<List<Country>> GetNormalizedCountries()
        {
            var dbCountries = await _context.Countries.Where(c => c.IsActive).ToListAsync();
            var normalizedCountries = new List<Country>();
            foreach (var country in dbCountries)
            {
                var defaultName = country.Names.GetValueOrDefault(Constants.DefaultLanguageCode);
                var code = _locationNameNormalizer.GetNormalizedCountryCode(defaultName, country.Code);
                var normalizedNames = _multilingualDataHelper.NormalizeCountryMultiLingualNames(country.Names);
                normalizedCountries.Add(new Country
                {
                    Id = country.Id,
                    Code = code,
                    Names = normalizedNames,
                    SupplierCountryCodes = country.SupplierCountryCodes,
                    IsActive = country.IsActive,
                    Modified = country.Modified
                });
            }

            return normalizedCountries;
        }


        public async Task<List<Locality>> GetNormalizedLocalitiesByCountry(string countryCode,
            CancellationToken cancellationToken)
        {
            var localities = await (from l in _context.Localities
                join c in _context.Countries on l.CountryId equals c.Id
                where l.IsActive && c.IsActive && c.Code == countryCode
                select new
                {
                    Locality = l,
                    CountryName = c.Names.En
                }).ToListAsync(cancellationToken);

            return localities.Select(l => new Locality
            {
                Id = l.Locality.Id,
                Names = _multilingualDataHelper.NormalizeLocalityMultilingualNames(l.CountryName, l.Locality.Names),
                CountryId = l.Locality.CountryId,
                SupplierLocalityCodes = l.Locality.SupplierLocalityCodes,
                Created = l.Locality.Created,
                Modified = l.Locality.Modified
            }).ToList();
        }


        public async Task<List<(string DefaultLocality, LocalityZone LocalityZone)>> GetNormalizedLocalityZonesByCountry(
            string countryCode, CancellationToken cancellationToken)
        {
            var localityZones = await (from lz in _context.LocalityZones
                join l in _context.Localities on lz.LocalityId equals l.Id
                join c in _context.Countries on l.CountryId equals c.Id
                where lz.IsActive && l.IsActive && c.IsActive && c.Code == countryCode
                select new
                {
                    LocalityName = l.Names.En,
                    Zone = lz
                }).ToListAsync(cancellationToken);

            return localityZones.Select(lz => (lz.LocalityName, new LocalityZone
            {
                Id = lz.Zone.Id,
                LocalityId = lz.Zone.LocalityId,
                Names = _multilingualDataHelper.NormalizeMultilingualNames(lz.Zone.Names),
                SupplierLocalityZoneCodes = lz.Zone.SupplierLocalityZoneCodes,
                Created = lz.Zone.Created,
                Modified = lz.Zone.Modified,
                IsActive = lz.Zone.IsActive
            })).ToList();
        }


        public Task<List<(string Code, int Id, string Name)>> GetCountries()
            => _context.Countries.Where(c => c.IsActive).Select(c => ValueTuple.Create(c.Code, c.Id, c.Names.En))
                .ToListAsync();


        private readonly MultilingualDataHelper _multilingualDataHelper;
        private readonly ILocationNameNormalizer _locationNameNormalizer;
        private readonly NakijinContext _context;
    }
}