using HappyTravel.Geography;

namespace HappyTravel.Nakijin.Api.Models.StaticDataPublications
{
    public readonly struct AccommodationData
    {
        public AccommodationData(int id, string name, string localityName, string countryName, string countryCode,
            GeoPoint coordinates)
        {
            Id = id;
            Name = name;
            LocalityName = localityName;
            CountryName = countryName;
            CountryCode = countryCode;
            Coordinates = coordinates;
        }

        public int Id { get; }
        public string Name { get; }
        public string LocalityName { get; }
        public string CountryName { get; }
        public string CountryCode { get; }
        public GeoPoint Coordinates { get; }
    }
}