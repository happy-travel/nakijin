using System;
using System.Collections.Generic;
using HappyTravel.MultiLanguage;

namespace HappyTravel.StaticDataMapper.Data.Models
{
    public class Locality
    {
        public int Id { get; set; }
        public int CountryId { get; set; }
        public MultiLanguage<string> Names { get; set; }
        public Dictionary<Suppliers,string> SupplierLocalityCodes { get; set; }
        public bool IsActive { get; set; }
        public DateTime Created { get; set; }
        public  DateTime Modified { get; set; }
    }
}