using System.Collections.Generic;
using System.Linq;
using HappyTravel.LocationNameNormalizer;

namespace HappyTravel.Nakijin.Api.Services.Validators
{
    public class LocationNameRetriever
    {
        public LocationNameRetriever(ILocationNameRetriever locationNameRetriever)
        {
            _locationNameRetriever = locationNameRetriever;
            InitSetOfEqualNames();
        }


        public bool CanCountryAndLocalityBeEqual(string name)
            => _equalCountryAndLocalityNameSet!.Contains(name);
            
        
        private void InitSetOfEqualNames()
        {
            _equalCountryAndLocalityNameSet = new HashSet<string>();
            foreach (var country in _locationNameRetriever.RetrieveCountries())
            {
                var primaryCountryName = country.Name.Primary;

                if (country.Localities.Select(l => l.Name.Primary).Contains(primaryCountryName))
                    _equalCountryAndLocalityNameSet.Add(primaryCountryName);
            }
        }
        

        /// <summary>
        /// Contains names of the localities that have the same name as the country
        /// </summary>
        private HashSet<string>? _equalCountryAndLocalityNameSet;

        private readonly ILocationNameRetriever _locationNameRetriever;
    }
}