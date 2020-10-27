using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace HappyTravel.PropertyManagement.Data.Models.Accommodations
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum AccommodationDataTypes
    {
        Name = 1,
        AccommodationAmenities = 2,
        AdditionalInfo = 3,
        Category = 4,
        ContactInfo = 5,
        LocationInfo = 6,
        Photos = 7,
        Rating = 8,
        RoomAmenities = 9,
        Schedule = 10,
        PropertyType = 11,
        TextualDescriptions = 12,
        TypeDescription = 13
    }
}