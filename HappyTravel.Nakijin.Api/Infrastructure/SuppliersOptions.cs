using System.Collections.Generic;
using HappyTravel.Nakijin.Data.Models;
using HappyTravel.SuppliersCatalog;

namespace HappyTravel.Nakijin.Api.Infrastructure
{
    public class SuppliersOptions
    {
        public Dictionary<Suppliers, string> SuppliersUrls { get; set; } = new Dictionary<Suppliers, string>();
    }
}