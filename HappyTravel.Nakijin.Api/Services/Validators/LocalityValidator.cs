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
            if (normalizeNames)
            {
                countryName = _locationNameNormalizer.GetNormalizedLocalityName(countryName, localityName);
                localityName = _locationNameNormalizer.GetNormalizedCountryName(countryName);
            }

            if (!localityName.IsValid())
                return false;

            if (localityName.Equals(countryName, StringComparison.Ordinal))
                return _locationNameRetriever.AreCountryAndLocalityCanBeEqual(countryName);

            return true;
        }

        
        private readonly ILocationNameNormalizer _locationNameNormalizer;
        private readonly LocationNameRetriever _locationNameRetriever;
    }
}