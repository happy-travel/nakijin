using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HappyTravel.StaticDataMapper.Api.Models.LocationInfo;
using HappyTravel.StaticDataMapper.Api.Models.LocationServiceInfo;
using HappyTravel.StaticDataMapper.Data;
using Microsoft.EntityFrameworkCore;

namespace HappyTravel.StaticDataMapper.Api.Services.LocationMappingInfo
{
    public class LocationMappingFactory : ILocationMappingFactory
    {
        public LocationMappingFactory(NakijinContext context)
        {
            _context = context;
        }


        public async Task<List<LocationMapping>> GetForCountry(List<int> countryIds, string languageCode)
        {
            var accommodationsInfo = await (from country in _context.Countries
                join accommodation in _context.Accommodations on country.Id equals accommodation.CountryId
                where countryIds.Contains(country.Id)
                select new
                {
                    CountryId = country.Id,
                    CountryNames = country.Names,
                    AccommodationId = accommodation.Id,
                    SupplierCodes = accommodation.SupplierAccommodationCodes
                }).ToListAsync();

            if (!accommodationsInfo.Any())
                return new List<LocationMapping>();

            return accommodationsInfo
                .GroupBy(a => a.CountryId)
                .Select(g =>
                {
                    return new LocationMapping
                    {
                        Location = new Models.LocationInfo.Location
                        {
                            Country = g
                                .Select(a => a.CountryNames.GetValueOrDefault(languageCode))
                                .FirstOrDefault() ?? string.Empty
                        },
                        AccommodationMappings = g.Select(a => new AccommodationMapping
                        {
                            HtId = HtId.Create(AccommodationMapperLocationTypes.Accommodation, a.AccommodationId),
                            SupplierCodes = a.SupplierCodes
                        }).ToList()
                    };
                })
                .ToList();
        }


        public async Task<List<LocationMapping>> GetForLocality(List<int> localityIds, string languageCode)
        {
            var accommodationsInfo = await (from country in _context.Countries
                join locality in _context.Localities on country.Id equals locality.CountryId
                join accommodation in _context.Accommodations on locality.Id equals accommodation.LocalityId
                where localityIds.Contains(locality.Id)
                select new
                {
                    LocalityId = locality.Id,
                    CountryNames = country.Names,
                    LocalityNames = locality.Names,
                    AccommodationId = accommodation.Id,
                    SupplierCodes = accommodation.SupplierAccommodationCodes
                }).ToListAsync();

            if (!accommodationsInfo.Any())
                return new List<LocationMapping>();

            return accommodationsInfo.GroupBy(a => a.LocalityId)
                .Select(g =>
                {
                    var firstAccommodationInfo = g.First();
                    return new LocationMapping
                    {
                        Location = new Models.LocationInfo.Location
                        {
                            Country = firstAccommodationInfo.CountryNames.GetValueOrDefault(languageCode),
                            Locality = firstAccommodationInfo.LocalityNames.GetValueOrDefault(languageCode)
                        },
                        AccommodationMappings = g.Select(a => new AccommodationMapping
                        {
                            HtId = HtId.Create(AccommodationMapperLocationTypes.Accommodation, a.AccommodationId),
                            SupplierCodes = a.SupplierCodes
                        }).ToList()
                    };
                })
                .ToList();
        }


        public async Task<List<LocationMapping>> GetForLocalityZone(List<int> localityZoneIds, string languageCode)
        {
            var accommodationsInfo = await (from country in _context.Countries
                join locality in _context.Localities on country.Id equals locality.CountryId
                join localityZone in _context.LocalityZones on locality.Id equals localityZone.LocalityId
                join accommodation in _context.Accommodations on localityZone.Id equals accommodation.LocalityZoneId
                where localityZoneIds.Contains(localityZone.Id)
                select new
                {
                    LocalityZoneId = localityZone.Id,
                    CountryNames = country.Names,
                    LocalityNames = locality.Names,
                    LocalityZoneNames = localityZone.Names,
                    AccommodationId = accommodation.Id,
                    SupplierCodes = accommodation.SupplierAccommodationCodes
                }).ToListAsync();

            if (!accommodationsInfo.Any())
                return new List<LocationMapping>();

            return accommodationsInfo.GroupBy(a => a.LocalityZoneId)
                .Select(g =>
                {
                    var firstAccommodationInfo = g.First();
                    return new LocationMapping
                    {
                        Location = new Models.LocationInfo.Location
                        {
                            Country = firstAccommodationInfo.CountryNames.GetValueOrDefault(languageCode),
                            Locality = firstAccommodationInfo.LocalityNames.GetValueOrDefault(languageCode),
                            Name = firstAccommodationInfo.LocalityZoneNames.GetValueOrDefault(languageCode)
                        },
                        AccommodationMappings = g.Select(a => new AccommodationMapping
                        {
                            HtId = HtId.Create(AccommodationMapperLocationTypes.Accommodation, a.AccommodationId),
                            SupplierCodes = a.SupplierCodes
                        }).ToList()
                    };
                })
                .ToList();
        }


        public async Task<List<LocationMapping>> GetForAccommodation(List<int> accommodationIds, string languageCode)
        {
            var accommodationsInfo = await (from accommodation in _context.Accommodations
                join country in _context.Countries on accommodation.CountryId equals country.Id
                join optionalLocality in _context.Localities on accommodation.LocalityId equals optionalLocality.Id into localities
                from locality in localities.DefaultIfEmpty() 
                where accommodationIds.Contains(accommodation.Id) && country.Id == accommodation.CountryId
                select new
                {
                    CountryNames = country.Names,
                    LocalityNames = locality.Names,
                    CalculatedAccommodation = accommodation.CalculatedAccommodation,
                    AccommodationId = accommodation.Id,
                    SupplierCodes = accommodation.SupplierAccommodationCodes,
                }).ToListAsync();

            if (!accommodationsInfo.Any())
                return new List<LocationMapping>();

            return accommodationsInfo.Select(a =>
            {
                return new LocationMapping
                {
                    Location = new Models.LocationInfo.Location
                    {
                        Country = a.CountryNames.GetValueOrDefault(languageCode),
                        Locality = a.LocalityNames?.GetValueOrDefault(languageCode),
                        Name = a.CalculatedAccommodation.Name.GetValueOrDefault(languageCode),
                        Coordinates = a.CalculatedAccommodation.Location.Coordinates
                    },
                    AccommodationMappings = new List<AccommodationMapping>
                    {
                        new()
                        {
                            HtId = HtId.Create(AccommodationMapperLocationTypes.Accommodation,
                                a.AccommodationId),
                            SupplierCodes = a.SupplierCodes
                        }
                    }
                };
            })
                .ToList();
            
        }


        private readonly NakijinContext _context;
    }
}