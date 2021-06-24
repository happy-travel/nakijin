using System;
using System.Collections.Generic;
using HappyTravel.EdoContracts.Accommodations;
using HappyTravel.SuppliersCatalog;

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

        public Dictionary<AccommodationDataTypes, List<Suppliers>> SuppliersPriority { get; set; } = new ();

        public Dictionary<Suppliers, string> SupplierAccommodationCodes { get; set; } = new ();
        public bool IsCalculated { get; set; }
        public bool IsActive { get; set; }    
        public DeactivationReasons DeactivationReason { get; set; }
        public DateTime Created { get; set; }
        public DateTime Modified { get; set; }

        public virtual Country Country { get; set; }
        public virtual Locality Locality { get; set; }
        public virtual LocalityZone LocalityZone { get; set; }
        public virtual ICollection<HtAccommodationMapping> HtAccommodationMappings { get; set; }
        public virtual ICollection<AccommodationUncertainMatches> SourceAccommodationUncertainMatches { get; set; }
        public virtual ICollection<AccommodationUncertainMatches> AccommodationToMatchUncertainMatches { get; set; }
    }
}