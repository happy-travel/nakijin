using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HappyTravel.MapperContracts.Internal.Mappings;
using HappyTravel.MapperContracts.Internal.Mappings.Enums;
using HappyTravel.MapperContracts.Internal.Mappings.Internals;
using HappyTravel.Nakijin.Data;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using Location = HappyTravel.MapperContracts.Internal.Mappings.Internals.Location;

namespace HappyTravel.Nakijin.Api.Services.LocationMappingInfo
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
                where countryIds.Contains(country.Id) && accommodation.IsActive
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
                    (
                        location : new Location
                        (
                            country: g
                                .Select(a => a.CountryNames.GetValueOrDefault(languageCode))
                                .FirstOrDefault() ?? string.Empty,
                            locality: string.Empty,
                            name: String.Empty,
                            coordinates: default,
                            type: MapperLocationTypes.Country
                        ),
                        accommodationMappings : g.Select(a => new AccommodationMapping
                        (
                            htId : HtId.Create(MapperLocationTypes.Accommodation, a.AccommodationId),
                            supplierCodes : a.SupplierCodes
                        )).ToList()
                    );
                })
                .ToList();
        }


        public async Task<List<LocationMapping>> GetForLocality(List<int> localityIds, string languageCode)
        {
            var accommodationsInfo = await (from country in _context.Countries
                join locality in _context.Localities on country.Id equals locality.CountryId
                join accommodation in _context.Accommodations on locality.Id equals accommodation.LocalityId
                where localityIds.Contains(locality.Id) && accommodation.IsActive
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
                    (
                        location : new Location
                        (
                            country : firstAccommodationInfo.CountryNames.GetValueOrDefault(languageCode),
                            locality : firstAccommodationInfo.LocalityNames.GetValueOrDefault(languageCode),
                            name : string.Empty,
                            coordinates: default,
                            type: MapperLocationTypes.Locality
                        ),
                        accommodationMappings : g.Select(a => new AccommodationMapping
                        (
                            htId : HtId.Create(MapperLocationTypes.Accommodation, a.AccommodationId),
                            supplierCodes : a.SupplierCodes
                        )).ToList()
                    );
                })
                .ToList();
        }


        public async Task<List<LocationMapping>> GetForLocalityZone(List<int> localityZoneIds, string languageCode)
        {
            var accommodationsInfo = await (from country in _context.Countries
                join locality in _context.Localities on country.Id equals locality.CountryId
                join localityZone in _context.LocalityZones on locality.Id equals localityZone.LocalityId
                join accommodation in _context.Accommodations on localityZone.Id equals accommodation.LocalityZoneId
                where localityZoneIds.Contains(localityZone.Id) && accommodation.IsActive
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
                    (
                        location: new Location
                        (
                            coordinates: default,
                            country: firstAccommodationInfo.CountryNames.GetValueOrDefault(languageCode),
                            locality: firstAccommodationInfo.LocalityNames.GetValueOrDefault(languageCode),
                            name: firstAccommodationInfo.LocalityZoneNames.GetValueOrDefault(languageCode),
                            type: MapperLocationTypes.LocalityZone
                        ),
                        accommodationMappings: g.Select(a => new AccommodationMapping
                        (
                            htId: HtId.Create(MapperLocationTypes.Accommodation, a.AccommodationId),
                            supplierCodes: a.SupplierCodes
                        )).ToList()
                    );
                })
                .ToList();
        }


        public async Task<List<LocationMapping>> GetForAccommodation(List<int> accommodationIds, string languageCode)
        {
            var accommodationsInfo = await (from accommodation in _context.Accommodations
                join country in _context.Countries on accommodation.CountryId equals country.Id
                join optionalLocality in _context.Localities on accommodation.LocalityId equals optionalLocality.Id into localities
                from locality in localities.DefaultIfEmpty()
                where accommodationIds.Contains(accommodation.Id) && country.Id == accommodation.CountryId && accommodation.IsActive
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
                    (
                        location: new Location
                        (
                            country: a.CountryNames.GetValueOrDefault(languageCode),
                            locality: a.LocalityNames?.GetValueOrDefault(languageCode),
                            name: a.CalculatedAccommodation.Name.GetValueOrDefault(languageCode),
                            coordinates: a.CalculatedAccommodation.Location.Coordinates,
                            type: MapperLocationTypes.Accommodation
                        ),
                        accommodationMappings: new List<AccommodationMapping>
                        {
                            new
                            (
                                htId: HtId.Create(MapperLocationTypes.Accommodation,
                                    a.AccommodationId),
                                supplierCodes: a.SupplierCodes
                            )
                        }
                    );
                })
                .ToList();
        }


        private readonly NakijinContext _context;
    }
}