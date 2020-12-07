using System.Collections.Generic;
using System.Text.Json;

namespace HappyTravel.StaticDataMapper.Data.Models
{
    public class Locality
    {
        public int Id { get; set; }
        public int CountryId { get; set; }
        public JsonDocument Names { get; set; }
        public Dictionary<Suppliers,string> SupplierLocalityCodes { get; set; }
        public bool IsActive { get; set; }
    }
}