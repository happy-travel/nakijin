using System.Collections.Generic;
using HappyTravel.Nakijin.Data.Models;
using HappyTravel.SuppliersCatalog;

namespace HappyTravel.Nakijin.Api.Models.LocationInfo
{
    public readonly struct AccommodationMapping
    {
        public string HtId { get; init; }
        public Dictionary<Suppliers, string> SupplierCodes { get; init; }
    }
}