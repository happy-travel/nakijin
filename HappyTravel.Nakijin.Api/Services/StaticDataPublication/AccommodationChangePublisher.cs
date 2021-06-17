using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HappyTravel.EdoContracts.GeoData.Enums;
using HappyTravel.MapperContracts.Internal.Mappings.Enums;
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


        public Task PublishAdd(AccommodationData addedAccommodation)
        {
            var convertedAccommodationAdded = Convert(addedAccommodation);

            return _staticDataPublicationService.Publish(convertedAccommodationAdded, UpdateEventTypes.Add);
        }


        public Task PublishRemove(int id)
        {
            var convertedAccommodationRemoved = Convert(id);

            return _staticDataPublicationService.Publish(convertedAccommodationRemoved, UpdateEventTypes.Remove);
        }


        public Task PublishAdd(List<AccommodationData> addedAccommodations)
        {
            if (!addedAccommodations.Any())
                return Task.CompletedTask;

            var convertedAccommodationsAdded = addedAccommodations.Select(Convert).ToList();

            return _staticDataPublicationService.Publish(convertedAccommodationsAdded, UpdateEventTypes.Add);
        }


        public Task PublishRemove(List<int> removedAccommodations)
        {
            if (!removedAccommodations.Any())
                return Task.CompletedTask;

            var convertedAccommodations = removedAccommodations.Select(Convert).ToList();

            return _staticDataPublicationService.Publish(convertedAccommodations, UpdateEventTypes.Remove);
        }


        public Task PublishUpdate(List<int> accommodationIds)
        {
            if (!accommodationIds.Any())
                return Task.CompletedTask;
            
            var convertedAccommodations = accommodationIds.Select(Convert).ToList();

            return _staticDataPublicationService.Publish(convertedAccommodations, UpdateEventTypes.Update);
        }
        
        
        private static Location Convert(AccommodationData accommodation)
            => new Location(
                HtId.Create(MapperLocationTypes.Accommodation, accommodation.Id),
                accommodation.Name,
                accommodation.LocalityName,
                accommodation.CountryName,
                accommodation.CountryCode,
                accommodation.Coordinates,
                distanceInMeters: 0,
                PredictionSources.Interior,
                MapperLocationTypes.Accommodation,
                LocationTypes.Accommodation);


        private static Location Convert(int id)
            => new Location(HtId.Create(MapperLocationTypes.Accommodation, id),
                name: string.Empty,
                locality: string.Empty,
                country: string.Empty,
                countryCode: string.Empty,
                GeoPointExtension.OriginGeoPoint,
                distanceInMeters: 0,
                PredictionSources.Interior,
                MapperLocationTypes.Accommodation,
                LocationTypes.Accommodation);


        private readonly IStaticDataPublicationService _staticDataPublicationService;
    }
}