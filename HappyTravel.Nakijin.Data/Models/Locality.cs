using System;
using System.Collections.Generic;
using HappyTravel.MultiLanguage;
using HappyTravel.Nakijin.Data.Models.Accommodations;
using HappyTravel.SuppliersCatalog;

namespace HappyTravel.Nakijin.Data.Models
{
    public class Locality
    {
        public int Id { get; set; }
        public int CountryId { get; set; }
        public MultiLanguage<string> Names { get; set; }
        public Dictionary<Suppliers, string> SupplierLocalityCodes { get; set; }
        public bool IsActive { get; set; }
        public DateTime Created { get; set; }
        public DateTime Modified { get; set; }
        
        public virtual Country Country { get; set; }
        public virtual ICollection<RichAccommodationDetails> Accommodations { get; set; }
        public virtual ICollection<LocalityZone> LocalityZones { get; set; }
    }
}