using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.EdoContracts.GeoData.Enums;
using HappyTravel.Geography;
using HappyTravel.Nakijin.Api.Models.LocationInfo;
using HappyTravel.Nakijin.Api.Models.LocationServiceInfo;
using HappyTravel.Nakijin.Data;
using Microsoft.EntityFrameworkCore;
using Location = HappyTravel.Nakijin.Api.Models.LocationServiceInfo.Location;

namespace HappyTravel.Nakijin.Api.Services.LocationMappingInfo
{
    public class LocationMappingInfoService : ILocationMappingInfoService
    {
        public LocationMappingInfoService(NakijinContext context, ILocationMappingFactory locationMappingFactory)
        {
            _context = context;
            _locationMappingFactory = locationMappingFactory;
        }

        
        public async Task<Result<List<LocationMapping>>> Get(List<string> htIds, string languageCode)
        {
            var parsedCodes = htIds
                .Select(HtId.Parse)
                .Where(c => c.IsSuccess)
                .Select(c => c.Value)
                .ToList();

            if (parsedCodes.Count != htIds.Count)
                return Result.Failure<List<LocationMapping>>("Some ids was not parsed");

            var parsedCodeGroups = parsedCodes
                .GroupBy(c => c.Type)
                .Select(g => new
                {
                    Type = g.Key,
                    Ids = g.Select(g => g.Id).ToList()
                });

            var mappings = new List<LocationMapping>();
            foreach (var parsedCodeGroup in parsedCodeGroups)
            {
                var ids = parsedCodeGroup.Ids;
                mappings.AddRange(parsedCodeGroup.Type switch
                {
                    AccommodationMapperLocationTypes.Country => await _locationMappingFactory.GetForCountry(ids, languageCode),
                    AccommodationMapperLocationTypes.Locality => await _locationMappingFactory.GetForLocality(ids, languageCode),
                    AccommodationMapperLocationTypes.LocalityZone => await _locationMappingFactory.GetForLocalityZone(ids, languageCode),
                    AccommodationMapperLocationTypes.Accommodation => await _locationMappingFactory.GetForAccommodation(ids, languageCode),
                    _ => throw new ArgumentOutOfRangeException()
                });
            }

            return mappings;
        }

        
        public Task<List<Location>> Get(AccommodationMapperLocationTypes locationType, string languageCode,
            DateTime modified, int skip, int top, CancellationToken cancellationToken = default)
        {
            return locationType switch
            {
                AccommodationMapperLocationTypes.Country => BuildCountries(languageCode, skip, top, modified, cancellationToken),
                AccommodationMapperLocationTypes.Locality => BuildLocalities(languageCode, skip, top, modified, cancellationToken),
                AccommodationMapperLocationTypes.LocalityZone => BuildLocalityZones(languageCode, skip, top, modified, cancellationToken),
                AccommodationMapperLocationTypes.Accommodation => BuildAccommodations(skip, top, modified, cancellationToken),
                _ => throw new ArgumentOutOfRangeException(nameof(locationType), locationType, null)
            };
        }

        
        private async Task<List<Location>> BuildCountries(string languageCode, int skip, int top, DateTime from,
            CancellationToken cancellationToken)
        {
            var countries = await _context.Countries.Where(c => c.IsActive && c.Modified >= from)
                .OrderBy(c => c.Id)
                .Skip(skip)
                .Take(top)
                .ToListAsync(cancellationToken);

            return countries.Select(c =>
                {
                    var country = c.Names.GetValueOrDefault(languageCode);
                    var htId = HtId.Create(AccommodationMapperLocationTypes.Country,
                        c.Id);

                    return new Location(htId, country, string.Empty, country, c.Code, EmptyGeoPoint, 0,
                        PredictionSources.Interior, AccommodationMapperLocationTypes.Country, LocationTypes.Location);
                })
                .ToList();
        }

        
        private async Task<List<Location>> BuildLocalities(string languageCode, int skip, int top, DateTime from,
            CancellationToken cancellationToken)
        {
            var locations = await _context.Localities
                .Join(_context.Countries, l => l.CountryId, c => c.Id, (locality, country) => new {locality, country})
                .OrderBy(lc => lc.locality.Id)
                .Where(lc => lc.locality.IsActive && lc.country.IsActive && lc.locality.Modified >= from)
                .Skip(skip)
                .Take(top)
                .ToListAsync(cancellationToken);

            return locations.Select(localityAndCountry =>
                {
                    var locality = localityAndCountry.locality.Names.GetValueOrDefault(languageCode);
                    var country = localityAndCountry.country.Names.GetValueOrDefault(languageCode);
                    var htId = HtId.Create(AccommodationMapperLocationTypes.Locality,
                        localityAndCountry.locality.Id);
                    
                    return new Location(htId, locality, locality, country,
                        localityAndCountry.country.Code, EmptyGeoPoint, 0, PredictionSources.Interior,
                        AccommodationMapperLocationTypes.Locality, LocationTypes.Location);
                })
                .ToList();
        }

        
        private async Task<List<Location>> BuildLocalityZones(string languageCode, int skip, int top, DateTime from,
            CancellationToken cancellationToken)
        {
            var zones = await _context.LocalityZones
                .Join(_context.Localities, lz => lz.LocalityId, l => l.Id, (zone, locality) => new {zone, locality})
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
                    var htId = HtId.Create(AccommodationMapperLocationTypes.LocalityZone,
                        zlc.zone.Id);

                    return new Location(htId, zone, locality, country, zlc.country.Code,
                        EmptyGeoPoint, 0,
                        PredictionSources.Interior, AccommodationMapperLocationTypes.LocalityZone,
                        LocationTypes.Location);
                })
                .ToList();
        }

        
        private async Task<List<Location>> BuildAccommodations(int skip, int top, DateTime from,
            CancellationToken cancellationToken)
        {
            var accommodations = await _context.Accommodations
                .GroupJoin(_context.Localities, a => a.LocalityId, l => l.Id, (accommodation, locality) => new {accommodation, locality})
                .SelectMany(al => al.locality.DefaultIfEmpty(), (al, locality) => new {al.accommodation, locality})
                .Join(_context.Countries, al => al.accommodation.CountryId, c => c.Id,
                    (al, country) => new {al.accommodation, al.locality, country})
                .Where(alc =>
                    alc.accommodation.IsActive && alc.country.IsActive && (alc.locality != null && alc.locality.IsActive || alc.locality == null) &&
                    alc.accommodation.Modified >= from)
                .OrderBy(alc => alc.accommodation.Id)
                .Skip(skip)
                .Take(top)
                .Select(alc => new
                {
                    alc.accommodation.Id,
                    alc.accommodation.MappingData,
                    alc.accommodation.CountryCode,
                })
                .ToListAsync(cancellationToken);
           
            return accommodations.Select(alc =>
                {
                    var accommodation = alc.MappingData.DefaultName;
                    var locality = alc.MappingData.DefaultLocalityName;
                    var country = alc.MappingData.DefaultCountryName;
                    var htId = HtId.Create(AccommodationMapperLocationTypes.Accommodation, alc.Id);
                    
                    return new Location(htId, accommodation, locality, country, alc.CountryCode,
                        alc.MappingData.Coordinates, 0,
                        PredictionSources.Interior, AccommodationMapperLocationTypes.Accommodation,
                        LocationTypes.Accommodation);
                })
                .ToList();
        }

        
        private static readonly GeoPoint EmptyGeoPoint = new(0, 0);
        private readonly NakijinContext _context;
        private readonly ILocationMappingFactory _locationMappingFactory;
    }
}