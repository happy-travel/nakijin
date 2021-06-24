namespace HappyTravel.Nakijin.Api.Services.Validators
{
    public interface ILocalityValidator
    {
        public bool IsNormalizedValid(string normalizedCountryName, string normalizedLocalityName);
    }
}