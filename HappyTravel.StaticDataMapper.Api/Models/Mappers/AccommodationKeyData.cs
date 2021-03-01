using System;
using System.Collections.Generic;
using HappyTravel.EdoContracts.Accommodations;
using HappyTravel.StaticDataMapper.Data.Models;
using HappyTravel.StaticDataMapper.Data.Models.Accommodations;

namespace HappyTravel.StaticDataMapper.Api.Models.Mappers
{
    public class AccommodationKeyData
    {
        public int HtId { get; set; }
        
        public AccommodationDataForMapping DataForMapping { get; set; }
        
        public Dictionary<Suppliers,string> SupplierAccommodationCodes { get; set; } = new Dictionary<Suppliers, string>();
    }
}