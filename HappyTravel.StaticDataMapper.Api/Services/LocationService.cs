using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HappyTravel.EdoContracts.GeoData.Enums;
using HappyTravel.Geography;
using HappyTravel.StaticDataMapper.Api.Models.LocationServiceInfo;
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


        public Task<List<Location>> Get(AccommodationMapperLocationTypes locationType, string languageCode, DateTime modified, int skip, int top, CancellationToken cancellationToken = default)
        {
            return locationType switch
            {
                AccommodationMapperLocationTypes.Country => BuildCountries(languageCode, skip, top, modified,
                    cancellationToken),
                AccommodationMapperLocationTypes.Locality => BuildLocalities(languageCode, skip, top, modified,
                    cancellationToken),
                AccommodationMapperLocationTypes.LocalityZone => BuildLocalityZones(languageCode, skip, top, modified,
                    cancellationToken),
                AccommodationMapperLocationTypes.Accommodation => BuildAccommodations(languageCode, skip, top, modified,
                    cancellationToken),
                _ => throw new ArgumentOutOfRangeException(nameof(locationType), locationType, null)
            };
        }


        private async Task<List<Location>> BuildCountries(string languageCode, int skip, int top, DateTime from, CancellationToken cancellationToken)
        {
            var countries = await _context.Countries
                .Where(c => c.IsActive && c.Modified >= from)
                .OrderBy(c => c.Id)
                .Skip(skip)
                .Take(top)
                .ToListAsync(cancellationToken);

            return countries.Select(c =>
            {
                var country = c.Names.GetValueOrDefault(languageCode);
                
                return new Location(c.Id.ToString(), 
                    country, string.Empty,
                    country,
                    c.Code,
                    EmptyGeoPoint,
                    0,
                    PredictionSources.Interior,
                    AccommodationMapperLocationTypes.Country,
                    LocationTypes.Location);
            }).ToList();
        }
        
        
        private async Task<List<Location>> BuildLocalities(string languageCode, int skip, int top, DateTime from, CancellationToken cancellationToken)
        {
            var locations = await _context.Localities.Join(_context.Countries, l => l.CountryId, c => c.Id,
                    (locality, country) => new {locality, country})
                .OrderBy(lc => lc.locality.Id)
                .Where(lc => lc.locality.IsActive && lc.country.IsActive && lc.locality.Modified >= from)
                .Skip(skip)
                .Take(top)
                .ToListAsync(cancellationToken);

            return locations.Select(localityAndCountry =>
            {
                var locality = localityAndCountry.locality.Names.GetValueOrDefault(languageCode);
                var country = localityAndCountry.country.Names.GetValueOrDefault(languageCode);
                
                return new Location(localityAndCountry.locality.Id.ToString(),
                    locality,
                    locality,
                    country,
                    localityAndCountry.country.Code,
                    EmptyGeoPoint,
                    0,
                    PredictionSources.Interior,
                    AccommodationMapperLocationTypes.Locality,
                    LocationTypes.Location);
            }).ToList();
        }
        

        private async Task<List<Location>> BuildLocalityZones(string languageCode, int skip, int top, DateTime from, CancellationToken cancellationToken)
        {
            var zones = await _context.LocalityZones.Join(_context.Localities, lz => lz.LocalityId, l => l.Id,
                    (zone, locality) => new {zone, locality})
                .Join(_context.Countries, zoneAndLocality => zoneAndLocality.locality.CountryId, c => c.Id,
                    (zl, country) => new {zl.zone, zl.locality, country})
                .Where(zlc =>
                    zlc.zone.IsActive && zlc.locality.IsActive && zlc.country.IsActive && zlc.zone.Modified >= from)
                .OrderBy(zlc => zlc.zone.Id)
                .Skip(skip)
                .Take(top)
                .ToListAsync(cancellationToken);

            return zones.Select(zlc =>
            {
                var zone = zlc.zone.Names.GetValueOrDefault(languageCode);
                var locality = zlc.locality.Names.GetValueOrDefault(languageCode);
                var country = zlc.country.Names.GetValueOrDefault(languageCode);

                return new Location(zlc.zone.Id.ToString(),
                    zone, 
                    locality, 
                    country, 
                    zlc.country.Code, 
                    EmptyGeoPoint, 
                    0, 
                    PredictionSources.Interior, 
                    AccommodationMapperLocationTypes.LocalityZone, 
                    LocationTypes.Location);
            }).ToList();
        }


        private async Task<List<Location>> BuildAccommodations(string languageCode, int skip, int top, DateTime from, CancellationToken cancellationToken)
        {
            var accommodations = await _context.Accommodations.Join(_context.Localities, a => a.LocalityId, l => l.Id,
                    (accommodation, locality) => new {accommodation, locality})
                .Join(_context.Countries, al => al.accommodation.CountryId, c => c.Id,
                    (al, country) => new {al.accommodation, al.locality, country})
                .Where(alс =>
                    alс.accommodation.IsActive && alс.locality.IsActive && alс.country.IsActive &&
                    alс.accommodation.Modified >= from)
                .OrderBy(alc => alc.accommodation.Id)
                .Skip(skip)
                .Take(top)
                .ToListAsync(cancellationToken);

            return accommodations.Select(alc =>
            {
                var accommodation = alc.accommodation.CalculatedAccommodation.Name.GetValueOrDefault(languageCode);
                var locality = alc.locality.Names.GetValueOrDefault(languageCode);
                var country = alc.country.Names.GetValueOrDefault(languageCode);

                return new Location(alc.accommodation.Id.ToString(),
                    accommodation,
                    locality,
                    country,
                    alc.country.Code,
                    alc.accommodation.AccommodationWithManualCorrections.Location.Coordinates,
                    0,
                    PredictionSources.Interior,
                    AccommodationMapperLocationTypes.Accommodation,
                    LocationTypes.Accommodation);
            }).ToList();
        }


        private static readonly GeoPoint EmptyGeoPoint = new(0, 0);
        private readonly NakijinContext _context;
    }
}