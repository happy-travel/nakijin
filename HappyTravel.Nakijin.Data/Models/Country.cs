using System;
using System.Collections.Generic;
using HappyTravel.MultiLanguage;
using HappyTravel.Nakijin.Data.Models.Accommodations;
using HappyTravel.SuppliersCatalog;

namespace HappyTravel.Nakijin.Data.Models
{
    public class Country
    {
        public int Id { get; set; }
        public string Code { get; set; }
        public MultiLanguage<string> Names { get; set; }
        public Dictionary<Suppliers, string> SupplierCountryCodes { get; set; }
        public bool IsActive { get; set; }
        public DateTime Created { get; set; }
        public DateTime Modified { get; set; }
        
        public virtual ICollection<Locality> Localities { get; set; }
        public virtual ICollection<RichAccommodationDetails> Accommodations { get; set; }
    }
}