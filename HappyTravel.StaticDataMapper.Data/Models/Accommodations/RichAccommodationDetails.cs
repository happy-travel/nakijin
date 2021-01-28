using System;
using System.Collections.Generic;
using HappyTravel.EdoContracts.Accommodations;

namespace HappyTravel.StaticDataMapper.Data.Models.Accommodations
{
    public class RichAccommodationDetails
    {
        public int Id { get; set; }
        public int CountryId { get; set; }
        public int? LocalityId { get; set; }
        public int? LocalityZoneId { get; set; }
        public string CountryCode { get; set; }
        public MultilingualAccommodation CalculatedAccommodation { get; set; }
        public MultilingualAccommodation AccommodationWithManualCorrections { get; set; }

        public Dictionary<AccommodationDataTypes, List<Suppliers>> SuppliersPriority { get; set; } =
            new Dictionary<AccommodationDataTypes, List<Suppliers>>();

        public Dictionary<Suppliers, string> SupplierAccommodationCodes { get; set; } =
            new Dictionary<Suppliers, string>();

        public bool IsCalculated { get; set; }
        public bool IsActive { get; set; }
        public DateTime Created { get; set; }
        public DateTime Modified { get; set; }
    }
}