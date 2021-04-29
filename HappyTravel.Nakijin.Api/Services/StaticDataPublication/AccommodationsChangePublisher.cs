using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HappyTravel.EdoContracts.GeoData.Enums;
using HappyTravel.Geography;
using HappyTravel.Nakijin.Api.Models.StaticDataPublications;
using HappyTravel.Nakijin.Api.Models.LocationServiceInfo;

namespace HappyTravel.Nakijin.Api.Services.StaticDataPublication
{
    public class AccommodationsChangePublisher
    {
        public AccommodationsChangePublisher(IStaticDataPublicationService staticDataPublicationService)
        {
            _staticDataPublicationService = staticDataPublicationService;
        }

        public async Task PublishAdded(AccommodationData addedAccommodation)
        {
            var convertedAccommodationAdded = new Location(
                HtId.Create(AccommodationMapperLocationTypes.Accommodation, addedAccommodation.Id),
                addedAccommodation.Name,
                addedAccommodation.LocalityName,
                addedAccommodation.CountryName,
                addedAccommodation.CountryCode,
                addedAccommodation.Coordinates,
                0,
                PredictionSources.Interior,
                AccommodationMapperLocationTypes.Accommodation,
                LocationTypes.Accommodation);

            await _staticDataPublicationService.Publish(convertedAccommodationAdded, UpdateEventTypes.Add);
        }

        public async Task PublishRemoved(int id)
        {
            var convertedAccommodationRemoved = new Location(
                HtId.Create(AccommodationMapperLocationTypes.Accommodation, id),
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                new GeoPoint(0, 0),
                0,
                PredictionSources.Interior,
                AccommodationMapperLocationTypes.Accommodation,
                LocationTypes.Accommodation);
            await _staticDataPublicationService.Publish(convertedAccommodationRemoved, UpdateEventTypes.Remove);
        }


        public async ValueTask PublishAdded(List<AccommodationData> addedAccommodations)
        {
            if (!addedAccommodations.Any())
                return;

            var convertedAccommodationsAdded = addedAccommodations.Select(ac => new Location(
                HtId.Create(AccommodationMapperLocationTypes.Accommodation, ac.Id),
                ac.Name,
                ac.LocalityName,
                ac.CountryName,
                ac.CountryCode,
                ac.Coordinates,
                0,
                PredictionSources.Interior,
                AccommodationMapperLocationTypes.Accommodation,
                LocationTypes.Accommodation)).ToList();

            await _staticDataPublicationService.Publish(convertedAccommodationsAdded, UpdateEventTypes.Add);
        }


        public async ValueTask PublishRemoved(List<int> removedAccommodations)
        {
            if (!removedAccommodations.Any())
                return;

            var convertedAccommodations = removedAccommodations.Select(ac
                => new Location(HtId.Create(AccommodationMapperLocationTypes.Accommodation, ac),
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    new GeoPoint(0, 0),
                    0,
                    PredictionSources.Interior,
                    AccommodationMapperLocationTypes.Accommodation,
                    LocationTypes.Accommodation)).ToList();

            await _staticDataPublicationService.Publish(convertedAccommodations, UpdateEventTypes.Remove);
        }


        private readonly IStaticDataPublicationService _staticDataPublicationService;
    }
}