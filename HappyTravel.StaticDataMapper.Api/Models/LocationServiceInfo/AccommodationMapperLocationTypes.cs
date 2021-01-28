using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace HappyTravel.StaticDataMapper.Api.Models.LocationServiceInfo
{
    [JsonConverter(typeof (StringEnumConverter))]
    public enum AccommodationMapperLocationTypes
    {
        Country = 1,
        Locality = 2,
        LocalityZone = 3,
        Accommodation = 4
    }
}