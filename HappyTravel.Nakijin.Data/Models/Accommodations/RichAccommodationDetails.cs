using System;
using System.Collections.Generic;
using HappyTravel.EdoContracts.Accommodations;

namespace HappyTravel.Nakijin.Data.Models.Accommodations
{
    public class RichAccommodationDetails
    {
        public int Id { get; set; }
        public int CountryId { get; set; }
        public int? LocalityId { get; set; }
        public int? LocalityZoneId { get; set; }
        public string CountryCode { get; set; }
        public AccommodationKeyData KeyData { get; set; }
        public MultilingualAccommodation CalculatedAccommodation { get; set; }
        
        public bool HasDirectContract { get; set; }
        public MultilingualAccommodation AccommodationWithManualCorrections { get; set; }

        public Dictionary<AccommodationDataTypes, List<Suppliers>> SuppliersPriority { get; set; } =
            new Dictionary<AccommodationDataTypes, List<Suppliers>>();

        public Dictionary<Suppliers, string> SupplierAccommodationCodes { get; set; } =
            new Dictionary<Suppliers, string>();

        public bool IsCalculated { get; set; }
        public bool IsActive { get; set; }    
        public DeactivationReasons DeactivationReason { get; set; }
        public DateTime Created { get; set; }
        public DateTime Modified { get; set; }


        public virtual ICollection<HtAccommodationMapping> HtAccommodationMappings { get; set; }
        public virtual ICollection<AccommodationUncertainMatches> SourceAccommodationUncertainMatches { get; set; }
        public virtual ICollection<AccommodationUncertainMatches> AccommodationToMatchUncertainMatches { get; set; }
    }
}