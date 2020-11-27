using System;
using System.Collections.Generic;
using HappyTravel.EdoContracts.Accommodations;
using HappyTravel.StaticDataMapper.Data.Models;

namespace HappyTravel.StaticDataMapper.Api.Models.Mappers
{
    public class AccommodationKeyData
    {
        public int HtId { get; set; }
        
        public Accommodation Data { get; set; }
        
        public Dictionary<Suppliers,string> SupplierAccommodationCodes { get; set; } = new Dictionary<Suppliers, string>();
    }
}