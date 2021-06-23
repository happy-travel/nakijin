using System;
using HappyTravel.LocationNameNormalizer;
using HappyTravel.Nakijin.Api.Infrastructure;

namespace HappyTravel.Nakijin.Api.Services.Validators
{
    public class LocalityValidator : ILocalityValidator
    {
        public LocalityValidator(ILocationNameNormalizer locationNameNormalizer, LocationNameRetriever locationNameRetriever)
        {
            _locationNameNormalizer = locationNameNormalizer;
            _locationNameRetriever = locationNameRetriever;
        }
        
        
        public bool IsValid(string countryName, string localityName, bool normalizeNames = false)
        {
            string normalizedCountryName;
            string normalizedLocalityName;
            
            if (normalizeNames)
            {
                normalizedLocalityName = _locationNameNormalizer.GetNormalizedLocalityName(countryName, localityName);
                normalizedCountryName = _locationNameNormalizer.GetNormalizedCountryName(countryName);
            }
            else
            {
                normalizedLocalityName = localityName;
                normalizedCountryName = countryName;
            }

            if (!normalizedLocalityName.IsValid())
                return false;

            if (normalizedLocalityName.Equals(normalizedCountryName, StringComparison.Ordinal))
                return _locationNameRetriever.CanCountryAndLocalityBeEqual(normalizedCountryName);

            return true;
        }

        
        private readonly ILocationNameNormalizer _locationNameNormalizer;
        private readonly LocationNameRetriever _locationNameRetriever;
    }
}