namespace HappyTravel.Nakijin.Api.Models.StaticDataPublications
{
    public readonly struct LocalityData
    {
        public LocalityData(int id, string name, string countryName, string countryCode)
        {
            Id = id;
            Name = name;
            CountryName = countryName;
            CountryCode = countryCode;
        }
        
        
        public int Id { get; }
        public string Name { get; }
        public string CountryName { get; }
        public string CountryCode { get; }
    }
}