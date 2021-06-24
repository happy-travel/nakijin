using System;
using HappyTravel.Nakijin.Api.Infrastructure;

namespace HappyTravel.Nakijin.Api.Services.Validators
{
    public class LocalityValidator : ILocalityValidator
    {
        public LocalityValidator(LocationNameRetriever locationNameRetriever)
        {
            _locationNameRetriever = locationNameRetriever;
        }
        
        
        public bool IsNormalizedValid(string normalizedCountryName, string normalizedLocalityName)
        {
            if (!normalizedLocalityName.IsValid())
                return false;

            if (normalizedLocalityName.Equals(normalizedCountryName, StringComparison.Ordinal))
                return _locationNameRetriever.CanCountryAndLocalityBeEqual(normalizedCountryName);

            return true;
        }
        
        private readonly LocationNameRetriever _locationNameRetriever;
    }
}