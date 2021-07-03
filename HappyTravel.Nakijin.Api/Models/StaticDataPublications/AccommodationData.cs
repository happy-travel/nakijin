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

        public override bool Equals(object? obj) => obj is AccommodationData other && Equals(other);


        public bool Equals(AccommodationData other)
            => (Id, Name, LocalityName, CountryName, CountryCode, Coordinates)
                .Equals((other.Id, other.Name, other.LocalityName, other.CountryName, other.CountryCode, other.Coordinates));


        public override int GetHashCode() => (Id, Name, LocalityName, CountryName, CountryCode, Coordinates).GetHashCode();
    }
}