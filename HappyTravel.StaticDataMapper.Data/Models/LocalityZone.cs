using System;
using System.Collections.Generic;
using HappyTravel.MultiLanguage;

namespace HappyTravel.StaticDataMapper.Data.Models
{
    public class LocalityZone
    {
        public int Id { get; set; }
        public int LocalityId { get; set; }
        public MultiLanguage<string> Names { get; set; }
        public Dictionary<Suppliers, string> SupplierLocalityZoneCodes { get; set; }
        public bool IsActive { get; set; }
        public DateTime Created { get; set; }
        public  DateTime Modified { get; set; }
    }
}