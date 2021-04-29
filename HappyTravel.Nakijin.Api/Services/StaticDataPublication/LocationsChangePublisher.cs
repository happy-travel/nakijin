using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HappyTravel.EdoContracts.GeoData.Enums;
using HappyTravel.Geography;
using HappyTravel.Nakijin.Api.Infrastructure;
using HappyTravel.Nakijin.Api.Models.LocationServiceInfo;
using HappyTravel.Nakijin.Api.Models.StaticDataPublications;


namespace HappyTravel.Nakijin.Api.Services.StaticDataPublication
{
    public class LocationsChangePublisher
    {
        public LocationsChangePublisher(IStaticDataPublicationService staticDataPublicationService)
        {
            _staticDataPublicationService = staticDataPublicationService;
        }


        public async ValueTask PublishAddedCountries(List<CountryData> addedCountries)
        {
            if (addedCountries.Count == 0)
                return;

            var convertedCountries = addedCountries.Select(c => new Location(
                HtId.Create(AccommodationMapperLocationTypes.Country, c.Id),
                string.Empty,
                string.Empty,
                c.Name,
                c.Code,
                GeoPointExtension.OriginGeoPoint,
                0,
                PredictionSources.Interior,
                AccommodationMapperLocationTypes.Country,
                LocationTypes.Location)).ToList();

            await _staticDataPublicationService.Publish(convertedCountries, UpdateEventTypes.Add);
        }


        public async ValueTask PublishRemovedCountries(List<int> removedCountries)
        {
            if (removedCountries.Count == 0)
                return;

            var convertedCountries = removedCountries.Select(c => new Location(
                HtId.Create(AccommodationMapperLocationTypes.Country, c),
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                GeoPointExtension.OriginGeoPoint,
                0,
                PredictionSources.Interior,
                AccommodationMapperLocationTypes.Country,
                LocationTypes.Location)).ToList();

            await _staticDataPublicationService.Publish(convertedCountries, UpdateEventTypes.Remove);
        }


        public async ValueTask PublishAddedLocalities(List<LocalityData> addedLocalities)
        {
            if (addedLocalities.Count == 0)
                return;

            var convertedLocalities = addedLocalities.Select(l
                => new Location(
                    HtId.Create(AccommodationMapperLocationTypes.Locality, l.Id),
                    l.Name,
                    l.Name,
                    l.CountryName,
                    l.CountryCode,
                    GeoPointExtension.OriginGeoPoint,
                    0,
                    PredictionSources.Interior,
                    AccommodationMapperLocationTypes.Locality,
                    LocationTypes.Location)).ToList();

            await _staticDataPublicationService.Publish(convertedLocalities, UpdateEventTypes.Add);
        }


        public async ValueTask PublishRemovedLocalities(List<int> removedLocalities)
        {
            if (removedLocalities.Count == 0)
                return;

            var convertedLocalities = removedLocalities.Select(l
                => new Location(
                    HtId.Create(AccommodationMapperLocationTypes.Locality, l),
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    GeoPointExtension.OriginGeoPoint,
                    0,
                    PredictionSources.Interior,
                    AccommodationMapperLocationTypes.Locality,
                    LocationTypes.Location)).ToList();

            await _staticDataPublicationService.Publish(convertedLocalities, UpdateEventTypes.Remove);
        }


        public async ValueTask PublishAddedLocalityZones(List<LocalityZoneData> addedLocalityZones)
        {
            if (addedLocalityZones.Count == 0)
                return;

            var convertedLocalityZones = addedLocalityZones.Select(lz
                => new Location(
                    HtId.Create(AccommodationMapperLocationTypes.LocalityZone, lz.Id),
                    lz.Name,
                    lz.LocalityName,
                    lz.CountryName,
                    lz.CountryCode,
                    GeoPointExtension.OriginGeoPoint,
                    0,
                    PredictionSources.Interior,
                    AccommodationMapperLocationTypes.LocalityZone,
                    LocationTypes.Location)).ToList();

            await _staticDataPublicationService.Publish(convertedLocalityZones, UpdateEventTypes.Add);
        }


        public async ValueTask PublishRemovedLocalityZones(List<int> removedLocalityZones)
        {
            if (removedLocalityZones.Count == 0)
                return;

            var convertedLocalityZones = removedLocalityZones.Select(lz
                => new Location(
                    HtId.Create(AccommodationMapperLocationTypes.LocalityZone, lz), string.Empty,
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    GeoPointExtension.OriginGeoPoint,
                    0,
                    PredictionSources.Interior,
                    AccommodationMapperLocationTypes.LocalityZone,
                    LocationTypes.Location)).ToList();

            await _staticDataPublicationService.Publish(convertedLocalityZones, UpdateEventTypes.Remove);
        }


        private readonly IStaticDataPublicationService _staticDataPublicationService;
    }
}