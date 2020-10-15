using System.Collections.Generic;

namespace HappyTravel.PropertyManagement.Data.Models.Accommodations
{
    public class WideAccommodationDetails
    {
        public int Id { get; set; }

        public string CountryCode { get; set; }
        public Accommodation Accommodation { get; set; }

        public Accommodation AccommodationWithManualCorrections { get; set; }

        public Dictionary<AccommodationDataTypes, List<Suppliers>> SuppliersPriority { get; set; }

        public Dictionary<Suppliers, string> SupplierAccommodationCodes { get; set; }

        public bool IsCalculated { get; set; }
    }
}