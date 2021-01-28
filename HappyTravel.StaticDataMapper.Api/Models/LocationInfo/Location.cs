using HappyTravel.Geography;

namespace HappyTravel.StaticDataMapper.Api.Models.LocationInfo
{
    public readonly struct Location
    {
        public GeoPoint Coordinates { get; init; }
        public string Country { get; init;}
        public string? Locality { get; init;}
        public string Name { get; init;}
    }
}