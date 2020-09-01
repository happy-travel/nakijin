using System.Collections.Generic;
using System.Text.Json;
using HappyTravel.EdoContracts.Accommodations.Enums;
using NetTopologySuite.Geometries;

namespace HappyTravel.PropertyManagement.Data.Models.Accommodations
{
    public class Accommodation
    {
        public Accommodation()
        {
            SupplierAccommodationCodes = new Dictionary<Suppliers, string>();
        }

        public int Id { get; set; }
        public string Name { get; set; }
        public string CountryCode { get; set; }
        public string Address { get; set; }
        public Point Coordinates { get; set; }
        public AccommodationRatings Rating { get; set; }
        public ContactInfo ContactInfo { get; set; }
        public Dictionary<Suppliers, string> SupplierAccommodationCodes { get; set; }
        public JsonDocument AccommodationDetails { get; set; }
    }
}