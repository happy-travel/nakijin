using System.Collections.Generic;
using HappyTravel.EdoContracts.GeoData.Enums;
using HappyTravel.Geography;
using HappyTravel.StaticDataMapper.Data.Models;

namespace HappyTravel.StaticDataMapper.Api.Models.Locations
{
    public readonly struct Location
    {
        public Location(string id, string name, string locality, string country, string countryCode, GeoPoint coordinates, double distanceInMeters, PredictionSources source, AccommodationMapperLocationTypes locationType, LocationTypes type, List<Suppliers> suppliers)
        {
            Id = id;
            Name = name;
            Locality = locality;
            Country = country;
            CountryCode = countryCode;
            Coordinates = coordinates;
            DistanceInMeters = distanceInMeters;
            Source = source;
            LocationType = locationType;
            Type = type;
            Suppliers = suppliers;
        }

        public string Id { get; }
        public string Name { get; }
        public string Locality { get; }
        public string Country { get; }
        public string CountryCode { get; }
        public GeoPoint Coordinates { get; }
        public double DistanceInMeters { get; }
        public PredictionSources Source { get; }
        public AccommodationMapperLocationTypes LocationType { get; }
        public LocationTypes Type { get; }
        public List<Suppliers> Suppliers { get; }
    }
}