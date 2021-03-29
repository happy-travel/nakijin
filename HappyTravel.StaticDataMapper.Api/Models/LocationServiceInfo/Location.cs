using HappyTravel.EdoContracts.GeoData.Enums;
using HappyTravel.Geography;

namespace HappyTravel.Nakijin.Api.Models.LocationServiceInfo
{
    public readonly struct Location
    {
        public Location(string htId, string name, string locality, string country, string countryCode, GeoPoint coordinates, double distanceInMeters, PredictionSources source, AccommodationMapperLocationTypes locationType, LocationTypes type)
        {
            HtId = htId;
            Name = name;
            Locality = locality;
            Country = country;
            CountryCode = countryCode;
            Coordinates = coordinates;
            DistanceInMeters = distanceInMeters;
            Source = source;
            LocationType = locationType;
            Type = type;
        }

        public string HtId { get; }
        public string Name { get; }
        public string Locality { get; }
        public string Country { get; }
        public string CountryCode { get; }
        public GeoPoint Coordinates { get; }
        public double DistanceInMeters { get; }
        public PredictionSources Source { get; }
        public AccommodationMapperLocationTypes LocationType { get; }
        public LocationTypes Type { get; }
    }
}