using System;
using System.Collections.Generic;
using HappyTravel.EdoContracts.Accommodations;
using HappyTravel.PropertyManagement.Data.Models;

namespace HappyTravel.PropertyManagement.Api.Models.Mappers
{
    public class AccommodationKeyData
    {
        public int HtId { get; set; }
        
        public Accommodation Data { get; set; }
        
        public Dictionary<Suppliers,string> SupplierAccommodationCodes { get; set; } = new Dictionary<Suppliers, string>();
    }
}