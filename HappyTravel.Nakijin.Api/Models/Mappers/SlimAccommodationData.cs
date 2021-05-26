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

        public AccommodationKeyData KeyData { get; set; } = new AccommodationKeyData();
        
        public Dictionary<Suppliers,string> SupplierAccommodationCodes { get; set; } = new Dictionary<Suppliers, string>();

        public bool IsActive { get; set; } = true;
        
        public DeactivationReasons DeactivationReason { get; set; }
    }
}