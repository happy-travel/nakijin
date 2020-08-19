using System.Text.Json;
using HappyTravel.PropertyManagement.Api.Models.Mappers.Enums;

namespace HappyTravel.PropertyManagement.Data.Models.Mappers
{
    public class RawAccommodation
    {
        public int Id { get; set; }
        public JsonDocument Accommodation { get; set; }
        public Suppliers Supplier { get; set; }
        public string SupplierAccommodationId { get; set; }
    }
}