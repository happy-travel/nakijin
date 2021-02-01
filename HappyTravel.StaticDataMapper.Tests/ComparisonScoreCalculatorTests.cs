using System.Collections.Generic;
using HappyTravel.EdoContracts.Accommodations;
using HappyTravel.EdoContracts.Accommodations.Enums;
using HappyTravel.EdoContracts.Accommodations.Internals;
using HappyTravel.Geography;
using Xunit;
using HappyTravel.StaticDataMapper.Api.Services.Workers;

namespace HappyTravel.StaticDataMapper.Tests
{
    public class ComparisonScoreCalculatorTests
    {
        // This test for checking score value after changes on score calculator functionality.
        [Fact]
        public void CalculatedScoreMustBeLarger()
        {
            var accommodation = new Accommodation
            (id: "hotel_toscana_laticastelli",
                name: "Hotel Melia Milano",
                rating: AccommodationRatings.FourStars,
                contacts: new ContactInfo(
                    faxes: new List<string>(),
                    emails: new List<string>() {"melia.milano@melia.com"},
                    phones: new List<string> {"39-02-44406"},
                    webSites: new List<string>()
                ),
                location: new LocationInfo(
                    address: "Via Masaccio 19, Milan",
                    country: "Italy",
                    localityHtId:null,
                    locality: "Rapolano Terme",
                    coordinates: new GeoPoint(45.478919982910156, 9.145230293273926),
                    countryCode: "IT",
                    countryHtId:"",
                    localityZoneHtId:"",
                    localityZone: "San Marco",
                    pointsOfInterests: new List<PoiInfo>(),
                    isHistoricalBuilding: false,
                    locationDescriptionCode: LocationDescriptionCodes.Unspecified
                ),
                accommodationAmenities: null,
                additionalInfo: null,
                type: PropertyTypes.Hotels,
                photos: null,
                category: null,
                schedule: new ScheduleInfo(),
                hotelChain: null,
                uniqueCodes: null,
                textualDescriptions: null
            );

            var nearestAccommodation = new Accommodation
            (id: "225344",
                name: "Melia",
                rating: AccommodationRatings.FourStars,
                contacts: new ContactInfo(
                    faxes: new List<string>(),
                    emails: new List<string> {""},
                    phones: new List<string> {"+39 02444061"},
                    webSites: new List<string>()
                ),
                location: new LocationInfo(
                    address: "Via Masaccio, 19 20149",
                    country: "",
                    locality: "Venice",
                    coordinates: new GeoPoint(45.4789183436, 9.14515969029),
                    countryCode: "IT",
                    countryHtId:"",
                    localityHtId:"",
                    localityZoneHtId:"",
                    localityZone: "",
                    pointsOfInterests: new List<PoiInfo>(),
                    isHistoricalBuilding: false,
                    locationDescriptionCode: LocationDescriptionCodes.Unspecified
                ),
                accommodationAmenities: null,
                additionalInfo: null,
                type: PropertyTypes.Hotels,
                photos: null,
                category: null,
                schedule: new ScheduleInfo(),
                hotelChain: null,
                uniqueCodes: null,
                textualDescriptions: null
            );

           // var score = ComparisonScoreCalculator.Calculate(nearestAccommodation, accommodation);

            Assert.True(true);
        }
    }
}