namespace HappyTravel.Nakijin.Api.Models.StaticDataPublications
{
    public readonly struct CountryData
    {
        public CountryData(int id, string name, string code)
        {
            Id = id;
            Name = name;
            Code = code;
        }

        public int Id { get; }
        public string Name { get; }
        public string Code { get; }
    }
}