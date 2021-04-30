using System;
using System.Collections.Generic;
using HappyTravel.EdoContracts.Accommodations;
using HappyTravel.Nakijin.Data.Models;
using HappyTravel.Nakijin.Data.Models.Accommodations;

namespace HappyTravel.Nakijin.Api.Models.Mappers
{
    public class SlimAccommodationData
    {
        public int HtId { get; set; }
        
        public AccommodationKeyData KeyData { get; set; }
        
        public Dictionary<Suppliers,string> SupplierAccommodationCodes { get; set; } = new Dictionary<Suppliers, string>();

        public bool IsActive { get; set; } = true;
    }
}