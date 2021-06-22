namespace HappyTravel.Nakijin.Api.Services.Validators
{
    public interface ILocalityValidator
    {
        public bool IsValid(string countryName, string localityName, bool normalizeNames = false);
    }
}