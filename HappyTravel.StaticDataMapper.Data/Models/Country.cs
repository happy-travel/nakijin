using System.Collections.Generic;
using System.Text.Json;
using HappyTravel.StaticDataMapper.Data.Models;

namespace HappyTravel.StaticDataMapper.Data.Models
{
    public class Country
    {
        public int Id { get; set; }
        public string Code { get; set; }
        public JsonDocument Names { get; set; }
        public Dictionary<Suppliers, string> SupplierCountryCodes { get; set; }
        public bool IsActive { get; set; }
    }
}