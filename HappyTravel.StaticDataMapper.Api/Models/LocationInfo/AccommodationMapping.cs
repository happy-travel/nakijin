using System.Collections.Generic;
using HappyTravel.StaticDataMapper.Data.Models;

namespace HappyTravel.StaticDataMapper.Api.Models.LocationInfo
{
    public readonly struct AccommodationMapping
    {
        public string HtId { get; init; }
        public Dictionary<Suppliers, string[]> SupplierCodes { get; init; }
    }
}