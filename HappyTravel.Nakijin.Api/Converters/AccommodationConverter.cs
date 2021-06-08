using System;
using System.Collections.Generic;
using System.Linq;
using HappyTravel.EdoContracts.Accommodations;
using HappyTravel.MapperContracts.Internal.Mappings.Enums;
using HappyTravel.MapperContracts.Public.Accommodations.Enums;
using HappyTravel.Nakijin.Api.Models.LocationServiceInfo;
using HappyTravel.Nakijin.Api.Services;
using Accommodation = HappyTravel.MapperContracts.Public.Accommodations.Accommodation;
using HappyTravel.MapperContracts.Public.Accommodations.Internals;
using EdoContractsInternals = HappyTravel.EdoContracts.Accommodations.Internals;
using EdoContractsEnums = HappyTravel.EdoContracts.Accommodations.Enums;

namespace HappyTravel.Nakijin.Api.Converters
{
    public static class AccommodationConverter
    {
        public static Accommodation Convert(int htId, int htCountryId, int? htLocalityId, int? htLocalityZoneId,
            MultilingualAccommodation accommodation, string language, DateTime modified)
        {
            var name = accommodation.Name.GetValueOrDefault(language);
            var accommodationAmenities = accommodation.AccommodationAmenities.GetValueOrDefault(language);
            var additionalInfo = accommodation.AdditionalInfo.GetValueOrDefault(language);
            var category = accommodation.Category.GetValueOrDefault(language);
            var address = accommodation.Location.Address.GetValueOrDefault(language);
            var localityName = accommodation.Location.Locality?.GetValueOrDefault(language);
            var countryName = accommodation.Location.Country.GetValueOrDefault(language);
            var localityZoneName = accommodation.Location.LocalityZone?.GetValueOrDefault(language);
            var textualDescriptions = new List<TextualDescription>();
            var accommodationHtId = HtId.Create(MapperLocationTypes.Accommodation, htId);
            var countryHtId = HtId.Create(MapperLocationTypes.Country, htCountryId);
            var localityHtId = htLocalityId is not null
                ? HtId.Create(MapperLocationTypes.Locality, htLocalityId.Value)
                : string.Empty;
            var localityZoneHtId = htLocalityZoneId is not null
                ? HtId.Create(MapperLocationTypes.LocalityZone, htLocalityZoneId.Value)
                : string.Empty;

            foreach (var descriptions in accommodation.TextualDescriptions)
            {
                var description = descriptions.Description.GetValueOrDefault(language);
                textualDescriptions.Add(new TextualDescription(GetTextualDescriptionType(descriptions.Type), description));
            }

            return new Accommodation(
                htId:htId.ToString(),
                name: name,
                accommodationAmenities: accommodationAmenities,
                additionalInfo: additionalInfo,
                category: category,
                contacts:GetContacts(accommodation.Contacts),
                location: new LocationInfo(
                    countryCode: accommodation.Location.CountryCode,
                    countryHtId: countryHtId,
                    country: countryName,
                    localityHtId: localityHtId,
                    locality: localityName,
                    localityZoneHtId: localityZoneHtId,
                    localityZone: localityZoneName,
                    coordinates: accommodation.Location.Coordinates,
                    address: address,
                    locationDescriptionCode: GetLocationDescriptionCode(accommodation.Location.LocationDescriptionCode),
                    pointsOfInterests: GetPoiInfos(accommodation.Location.PointsOfInterests),
                    isHistoricalBuilding: accommodation.Location.IsHistoricalBuilding
                ),
                photos: GetPhotos(accommodation.Photos),
                rating: GetRating(accommodation.Rating),
                schedule: GetScheduleInfo(accommodation.Schedule),
                textualDescriptions: textualDescriptions,
                type:GetPropertyType(accommodation.Type),
                modified: modified
            );
        }


        private static ContactInfo GetContacts(EdoContractsInternals.ContactInfo contactInfo)
            => new ContactInfo(
                emails: contactInfo.Emails,
                faxes: contactInfo.Faxes,
                phones: contactInfo.Phones,
                webSites: contactInfo.WebSites);


        private static List<ImageInfo> GetPhotos(List<EdoContractsInternals.ImageInfo> photos)
            => photos.Select(photo => new ImageInfo(caption: photo.Caption, sourceUrl: photo.SourceUrl))
                .ToList();


        private static LocationDescriptionCodes GetLocationDescriptionCode(EdoContractsEnums.LocationDescriptionCodes descriptionCode)
            => descriptionCode switch
            {
                EdoContractsEnums.LocationDescriptionCodes.Airport => LocationDescriptionCodes.Airport,
                EdoContractsEnums.LocationDescriptionCodes.Boutique => LocationDescriptionCodes.Boutique,
                EdoContractsEnums.LocationDescriptionCodes.City => LocationDescriptionCodes.City,
                EdoContractsEnums.LocationDescriptionCodes.Desert => LocationDescriptionCodes.Desert,
                EdoContractsEnums.LocationDescriptionCodes.Island => LocationDescriptionCodes.Island,
                EdoContractsEnums.LocationDescriptionCodes.Mountains => LocationDescriptionCodes.Mountains,
                EdoContractsEnums.LocationDescriptionCodes.Peripherals => LocationDescriptionCodes.Peripherals,
                EdoContractsEnums.LocationDescriptionCodes.Port => LocationDescriptionCodes.Port,
                EdoContractsEnums.LocationDescriptionCodes.Ranch => LocationDescriptionCodes.Ranch,
                EdoContractsEnums.LocationDescriptionCodes.CityCenter => LocationDescriptionCodes.CityCenter,
                EdoContractsEnums.LocationDescriptionCodes.OceanFront => LocationDescriptionCodes.OceanFront,
                EdoContractsEnums.LocationDescriptionCodes.OpenCountry => LocationDescriptionCodes.OpenCountry,
                EdoContractsEnums.LocationDescriptionCodes.RailwayStation => LocationDescriptionCodes.RailwayStation,
                EdoContractsEnums.LocationDescriptionCodes.WaterFront => LocationDescriptionCodes.WaterFront,
                EdoContractsEnums.LocationDescriptionCodes.SeaOrBeach => LocationDescriptionCodes.SeaOrBeach,
                EdoContractsEnums.LocationDescriptionCodes.CloseToCityCentre => LocationDescriptionCodes.CloseToCityCentre,
                EdoContractsEnums.LocationDescriptionCodes.Unspecified => LocationDescriptionCodes.Unspecified,
                _ => throw new NotSupportedException()
            };


        private static List<PoiInfo> GetPoiInfos(List<EdoContractsInternals.PoiInfo> poiInfos)
            => poiInfos.Select(poiInfo => new PoiInfo(
                name: poiInfo.Name,
                type: GetPoiType(poiInfo.Type),
                distance: poiInfo.Distance,
                time: poiInfo.Time)).ToList();


        private static PoiTypes GetPoiType(EdoContractsEnums.PoiTypes poiType)
            => poiType switch
            {
                EdoContractsEnums.PoiTypes.Airport => PoiTypes.Airport,
                EdoContractsEnums.PoiTypes.Bus => PoiTypes.Bus,
                EdoContractsEnums.PoiTypes.Center => PoiTypes.Center,
                EdoContractsEnums.PoiTypes.Fair => PoiTypes.Fair,
                EdoContractsEnums.PoiTypes.Metro => PoiTypes.Metro,
                EdoContractsEnums.PoiTypes.Station => PoiTypes.Station,
                EdoContractsEnums.PoiTypes.PointOfInterest => PoiTypes.PointOfInterest,
                _ => throw new NotSupportedException()
            };


        private static AccommodationRatings GetRating(EdoContractsEnums.AccommodationRatings rating)
            => rating switch
            {
                EdoContractsEnums.AccommodationRatings.OneStar => AccommodationRatings.OneStar,
                EdoContractsEnums.AccommodationRatings.TwoStars => AccommodationRatings.TwoStars,
                EdoContractsEnums.AccommodationRatings.ThreeStars => AccommodationRatings.ThreeStars,
                EdoContractsEnums.AccommodationRatings.FourStars => AccommodationRatings.FourStars,
                EdoContractsEnums.AccommodationRatings.FiveStars => AccommodationRatings.FiveStars,
                // TODO: Check data in connectors and change to Exception
                _ => 0,
            };


        private static ScheduleInfo GetScheduleInfo(EdoContractsInternals.ScheduleInfo scheduleInfo)
            => new ScheduleInfo(
                checkInTime: scheduleInfo.CheckInTime,
                checkOutTime: scheduleInfo.CheckOutTime,
                portersStartTime: scheduleInfo.PortersStartTime,
                portersEndTime: scheduleInfo.PortersEndTime,
                roomServiceStartTime: scheduleInfo.RoomServiceStartTime,
                roomServiceEndTime: scheduleInfo.RoomServiceEndTime
            );


        private static PropertyTypes GetPropertyType(EdoContractsEnums.PropertyTypes type)
            => type switch
            {
                EdoContractsEnums.PropertyTypes.Apartments => PropertyTypes.Apartments,
                EdoContractsEnums.PropertyTypes.Hotels => PropertyTypes.Hotels,
                _ => throw new NotSupportedException()
            };


        private static TextualDescriptionTypes GetTextualDescriptionType(EdoContractsEnums.TextualDescriptionTypes textualDescriptionType)
            => textualDescriptionType switch
            {
                EdoContractsEnums.TextualDescriptionTypes.Exterior => TextualDescriptionTypes.Exterior,
                EdoContractsEnums.TextualDescriptionTypes.General => TextualDescriptionTypes.General,
                EdoContractsEnums.TextualDescriptionTypes.Lobby => TextualDescriptionTypes.Lobby,
                EdoContractsEnums.TextualDescriptionTypes.Position => TextualDescriptionTypes.Position,
                EdoContractsEnums.TextualDescriptionTypes.Restaurant => TextualDescriptionTypes.Restaurant,
                EdoContractsEnums.TextualDescriptionTypes.Room => TextualDescriptionTypes.Room,
                EdoContractsEnums.TextualDescriptionTypes.Unspecified => TextualDescriptionTypes.Unspecified,
                _ => throw new NotSupportedException()
            };
    }
}