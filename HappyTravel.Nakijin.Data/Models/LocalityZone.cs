using System;
using System.Collections.Generic;
using HappyTravel.MultiLanguage;
using HappyTravel.Nakijin.Data.Models.Accommodations;
using HappyTravel.SuppliersCatalog;

namespace HappyTravel.Nakijin.Data.Models
{
    public class LocalityZone
    {
        public int Id { get; set; }
        public int LocalityId { get; set; }
        public MultiLanguage<string> Names { get; set; }
        public Dictionary<Suppliers, string> SupplierLocalityZoneCodes { get; set; }
        public bool IsActive { get; set; }
        public DateTime Created { get; set; }
        public DateTime Modified { get; set; }
        
        public virtual Locality Locality { get; set; }
        public virtual ICollection<RichAccommodationDetails> Accommodations { get; set; }
    }
}