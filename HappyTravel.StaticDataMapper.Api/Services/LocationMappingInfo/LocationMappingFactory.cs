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


        public async Task<LocationMapping> GetForCountry(int id, string languageCode)
        {
            var accommodationsInfo = await (from country in _context.Countries
                join accommodation in _context.Accommodations on country.Id equals accommodation.CountryId
                where country.Id == id
                select new
                {
                    CountryNames = country.Names,
                    AccommodationId = accommodation.Id,
                    SupplierCodes = accommodation.SupplierAccommodationCodes
                }).ToListAsync();

            if (!accommodationsInfo.Any())
            {
                return new LocationMapping
                {
                    Location = new Models.LocationInfo.Location(),
                    AccommodationMappings = new List<AccommodationMapping>()
                };
            }

            return new LocationMapping
            {
                Location = new Models.LocationInfo.Location
                {
                    Country = accommodationsInfo
                        .Select(a => a.CountryNames.GetValueOrDefault(languageCode))
                        .FirstOrDefault() ?? string.Empty
                },
                AccommodationMappings = accommodationsInfo.Select(a => new AccommodationMapping
                {
                    HtId = HtId.Create(AccommodationMapperLocationTypes.Accommodation, a.AccommodationId),
                    SupplierCodes = a.SupplierCodes
                }).ToList()
            };
        }


        public async Task<LocationMapping> GetForLocality(int id, string languageCode)
        {
            var accommodationsInfo = await (from country in _context.Countries
                join locality in _context.Localities on country.Id equals locality.CountryId
                join accommodation in _context.Accommodations on locality.Id equals accommodation.LocalityId
                where locality.Id == id
                select new
                {
                    CountryNames = country.Names,
                    LocalityNames = locality.Names,
                    AccommodationId = accommodation.Id,
                    SupplierCodes = accommodation.SupplierAccommodationCodes
                }).ToListAsync();

            if (!accommodationsInfo.Any())
            {
                return new LocationMapping
                {
                    Location = new Models.LocationInfo.Location(),
                    AccommodationMappings = new List<AccommodationMapping>()
                };
            }

            var firstAccommodationInfo = accommodationsInfo.First();
            return new LocationMapping
            {
                Location = new Models.LocationInfo.Location
                {
                    Country = firstAccommodationInfo.CountryNames.GetValueOrDefault(languageCode),
                    Locality = firstAccommodationInfo.LocalityNames.GetValueOrDefault(languageCode)
                },
                AccommodationMappings = accommodationsInfo.Select(a => new AccommodationMapping
                {
                    HtId = HtId.Create(AccommodationMapperLocationTypes.Accommodation, a.AccommodationId),
                    SupplierCodes = a.SupplierCodes
                }).ToList()
            };
        }


        public async Task<LocationMapping> GetForLocalityZone(int id, string languageCode)
        {
            var accommodationsInfo = await (from country in _context.Countries
                join locality in _context.Localities on country.Id equals locality.CountryId
                join localityZone in _context.LocalityZones on locality.Id equals localityZone.LocalityId
                join accommodation in _context.Accommodations on localityZone.Id equals accommodation.LocalityZoneId
                where localityZone.Id == id
                select new
                {
                    CountryNames = country.Names,
                    LocalityNames = locality.Names,
                    LocalityZoneNames = localityZone.Names,
                    AccommodationId = accommodation.Id,
                    SupplierCodes = accommodation.SupplierAccommodationCodes
                }).ToListAsync();

            if (!accommodationsInfo.Any())
            {
                return new LocationMapping
                {
                    Location = new Models.LocationInfo.Location(),
                    AccommodationMappings = new List<AccommodationMapping>()
                };
            }

            var firstAccommodationInfo = accommodationsInfo.First();
            return new LocationMapping
            {
                Location = new Models.LocationInfo.Location
                {
                    Country = firstAccommodationInfo.CountryNames.GetValueOrDefault(languageCode),
                    Locality = firstAccommodationInfo.LocalityNames.GetValueOrDefault(languageCode),
                    Name = firstAccommodationInfo.LocalityZoneNames.GetValueOrDefault(languageCode)
                },
                AccommodationMappings = accommodationsInfo.Select(a => new AccommodationMapping
                {
                    HtId = HtId.Create(AccommodationMapperLocationTypes.Accommodation, a.AccommodationId),
                    SupplierCodes = a.SupplierCodes
                }).ToList()
            };
        }


        public async Task<LocationMapping> GetForAccommodation(int id, string languageCode)
        {
            var accommodationInfo = await (from accommodation in _context.Accommodations
                join country in _context.Countries on accommodation.CountryId equals country.Id
                from locality in _context.Localities.Where(l => l.Id == accommodation.LocalityId).DefaultIfEmpty()
                where accommodation.Id == id && country.Id == accommodation.CountryId
                select new
                {
                    CountryNames = country.Names,
                    LocalityNames = locality.Names,
                    CalculatedAccommodation = accommodation.CalculatedAccommodation,
                    AccommodationId = accommodation.Id,
                    SupplierCodes = accommodation.SupplierAccommodationCodes,
                }).SingleOrDefaultAsync();

            if (accommodationInfo is null)
            {
                return new LocationMapping
                {
                    Location = new Models.LocationInfo.Location(),
                    AccommodationMappings = new List<AccommodationMapping>()
                };
            }

            return new LocationMapping
            {
                Location = new Models.LocationInfo.Location
                {
                    Country = accommodationInfo.CountryNames.GetValueOrDefault(languageCode),
                    Locality = accommodationInfo.LocalityNames?.GetValueOrDefault(languageCode),
                    Name = accommodationInfo.CalculatedAccommodation.Name.GetValueOrDefault(languageCode),
                    Coordinates = accommodationInfo.CalculatedAccommodation.Location.Coordinates
                },
                AccommodationMappings = new List<AccommodationMapping>
                {
                    new()
                    {
                        HtId = HtId.Create(AccommodationMapperLocationTypes.Accommodation,
                            accommodationInfo.AccommodationId),
                        SupplierCodes = accommodationInfo.SupplierCodes
                    }
                }
            };
        }


        private readonly NakijinContext _context;
    }
}