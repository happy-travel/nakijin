using System.Text.Json;

namespace HappyTravel.StaticDataMapper.Data.Models.Mappers
{
    public class RawAccommodation
    {
        public int Id { get; set; }
        public string CountryCode { get; set; }
        public JsonDocument Accommodation { get; set; }
        public Suppliers Supplier { get; set; }
        public string SupplierAccommodationId { get; set; }
    }
}