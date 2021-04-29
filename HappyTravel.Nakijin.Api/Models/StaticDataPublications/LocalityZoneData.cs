namespace HappyTravel.Nakijin.Api.Models.StaticDataPublications
{
    public readonly  struct LocalityZoneData
    {
        public LocalityZoneData(int id, string name, string localityName, string countryName,string countryCode)
        {
            Id = id;
            Name = name;
            LocalityName = localityName;
            CountryName = countryName;
            CountryCode = countryCode;
        }
        
        
        public int Id { get; }
        public string Name { get; }
        public string LocalityName { get; }
        public string CountryName { get; }
        public string CountryCode { get; }
    }
}