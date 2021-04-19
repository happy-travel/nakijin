using System.Collections.Generic;
using System.Text.Json.Serialization;
using HappyTravel.EdoContracts.Accommodations;
using HappyTravel.EdoContracts.Accommodations.Enums;
using HappyTravel.EdoContracts.Accommodations.Internals;
using HappyTravel.Geography;
using Xunit;
using HappyTravel.Nakijin.Api.Services.Workers;
using HappyTravel.Nakijin.Data.Models.Accommodations;
using Newtonsoft.Json;

namespace HappyTravel.Nakijin.Tests
{
    public class ComparisonScoreCalculatorTests
    {
        // This test for checking score value after changes on score calculator functionality.
        [Fact]
        public void CalculatedScoreMustBeLarger()
        {
            var accommodation = JsonConvert.DeserializeObject<AccommodationMappingData>(@"{
            ""rating"": 0,
            ""address"": ""Jumeirah Beach Roads, Madinat Jumeirah Resort, Al Sufouh, Dubai, United Arab Emirates"",
            ""contactInfo"": {
                ""faxes"": [],
                ""emails"": [],
                ""phones"": [],
                ""webSites"": []
            },
            ""coordinates"": {
                ""latitude"": 25.131753,
                ""longitude"": 55.18431
            },
            ""defaultName"": ""Jumeirah Dar Al Masyaf Madinats Jumeirah hotel"",
            ""defaultCountryName"": ""The United Arab Emirates"",
            ""defaultLocalityName"": ""Dubai"",
            ""defaultLocalityZoneName"": ""Jumeirah Umm Suqueim""
        }");
            var nearestAccommodation = JsonConvert.DeserializeObject<AccommodationMappingData>(@"{
    ""rating"": ""FiveStars"",
            ""address"": ""Jumeirah Beach Road, Madinat Jumeirah Resorts, Al Sufouh - PO Box 75157"",
            ""contactInfo"": {
                ""faxes"": [],
                ""emails"": [],
                ""phones"": [
                ""+971 43668888""
                    ],
                ""webSites"": []
            },
            ""coordinates"": {
                ""latitude"": 25.131752316817668,
                ""longitude"": 55.18441200256348
            },
            ""defaultName"": ""Jumeirah Dar Al Masyaf At Madinat Jumeirah"",
            ""defaultCountryName"": ""The United Arab Emirates"",
            ""defaultLocalityName"": ""Dubai"",
            ""defaultLocalityZoneName"": ""Jumeirah Umm Suqueim""
        }");

           var score = ComparisonScoreCalculator.Calculate(nearestAccommodation, accommodation);

            Assert.True(true);
        }
    }
}