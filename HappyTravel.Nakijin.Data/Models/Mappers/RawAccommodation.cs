using System;
using System.Text.Json;
using HappyTravel.MultiLanguage;

namespace HappyTravel.Nakijin.Data.Models.Mappers
{
    // TODO change name 
    public class RawAccommodation
    {
        public int Id { get; set; }
        public string CountryCode { get; set; }

        public MultiLanguage<string> CountryNames { get; set; }
        public string SupplierLocalityCode { get; set; }
        public MultiLanguage<string> LocalityNames { get; set; }
        public string SupplierLocalityZoneCode { get; set; }
        public MultiLanguage<string> LocalityZoneNames { get; set; }

        // TODO: think about to change to MultLingualAccommodation
        public JsonDocument Accommodation { get; set; }

        public Suppliers Supplier { get; set; }
        public string SupplierAccommodationId { get; set; }

        public DateTime Created { get; set; }
        public DateTime Modified { get; set; }
    }
}