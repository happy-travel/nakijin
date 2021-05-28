using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HappyTravel.EdoContracts.GeoData.Enums;
using HappyTravel.Nakijin.Api.Infrastructure;
using HappyTravel.Nakijin.Api.Models.StaticDataPublications;
using HappyTravel.Nakijin.Api.Models.LocationServiceInfo;

namespace HappyTravel.Nakijin.Api.Services.StaticDataPublication
{
    public class AccommodationChangePublisher
    {
        public AccommodationChangePublisher(IStaticDataPublicationService staticDataPublicationService)
        {
            _staticDataPublicationService = staticDataPublicationService;
        }


        public async Task PublishAdded(AccommodationData addedAccommodation)
        {
            var convertedAccommodationAdded = ConvertToLocation(addedAccommodation);

            await _staticDataPublicationService.Publish(convertedAccommodationAdded, UpdateEventTypes.Add);
        }


        public async Task PublishRemoved(int id)
        {
            var convertedAccommodationRemoved = ConvertToLocation(id);

            await _staticDataPublicationService.Publish(convertedAccommodationRemoved, UpdateEventTypes.Remove);
        }


        public async ValueTask PublishAdded(List<AccommodationData> addedAccommodations)
        {
            if (!addedAccommodations.Any())
                return;

            var convertedAccommodationsAdded = addedAccommodations.Select(ConvertToLocation).ToList();

            await _staticDataPublicationService.Publish(convertedAccommodationsAdded, UpdateEventTypes.Add);
        }


        public async ValueTask PublishRemoved(List<int> removedAccommodations)
        {
            if (!removedAccommodations.Any())
                return;

            var convertedAccommodations = removedAccommodations.Select(ConvertToLocation).ToList();

            await _staticDataPublicationService.Publish(convertedAccommodations, UpdateEventTypes.Remove);
        }

        private static Location ConvertToLocation(AccommodationData accommodation)
            => new Location(
                HtId.Create(AccommodationMapperLocationTypes.Accommodation, accommodation.Id),
                accommodation.Name,
                accommodation.LocalityName,
                accommodation.CountryName,
                accommodation.CountryCode,
                accommodation.Coordinates,
                distanceInMeters: 0,
                PredictionSources.Interior,
                AccommodationMapperLocationTypes.Accommodation,
                LocationTypes.Accommodation);


        private static Location ConvertToLocation(int id)
            => new Location(HtId.Create(AccommodationMapperLocationTypes.Accommodation, id),
                name: string.Empty,
                locality: string.Empty,
                country: string.Empty,
                countryCode: string.Empty,
                GeoPointExtension.OriginGeoPoint,
                distanceInMeters: 0,
                PredictionSources.Interior,
                AccommodationMapperLocationTypes.Accommodation,
                LocationTypes.Accommodation);


        private readonly IStaticDataPublicationService _staticDataPublicationService;
    }
}