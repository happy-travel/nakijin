using HappyTravel.EdoContracts.Accommodations.Enums;
using HappyTravel.EdoContracts.Accommodations.Internals;
using HappyTravel.Geography;

namespace HappyTravel.Nakijin.Data.Models.Accommodations
{
    public class AccommodationMappingData
    {
        public string DefaultName { get; set; }
        public GeoPoint Coordinates { get; set; }
        public string DefaultCountryName { get; set; }
        public string DefaultLocalityName { get; set; }
        public string DefaultLocalityZoneName { get; set; }
        public string Address { get; set; }
        public AccommodationRatings Rating { get; set; }
        public ContactInfo ContactInfo { get; set; }
    }
}