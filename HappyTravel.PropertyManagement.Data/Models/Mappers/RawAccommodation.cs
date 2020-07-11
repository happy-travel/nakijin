using System.Text.Json;

namespace HappyTravel.PropertyManagement.Data.Models.Mappers
{
    public class RawAccommodation
    {
        public int Id { get; set; }
        public JsonDocument Accommodation { get; set; }
        public string Supplier { get; set; }
        public string SupplierId { get; set; }
    }
}
