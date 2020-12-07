using System.Collections.Generic;
using System.Text.Json;

namespace HappyTravel.StaticDataMapper.Data.Models
{
    public class LocalityZone
    {
        public int Id { get; set; }
        public int LocalityId { get; set; }
        public JsonDocument Names { get; set; }
        public Dictionary<Suppliers, string> SupplierLocalityZoneCodes { get; set; }
        public bool IsActive { get; set; }
    }
}