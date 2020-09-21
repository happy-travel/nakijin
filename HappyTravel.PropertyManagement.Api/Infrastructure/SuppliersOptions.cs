using System.Collections.Generic;
using HappyTravel.PropertyManagement.Data.Models;

namespace HappyTravel.PropertyManagement.Api.Infrastructure
{
    public class SuppliersOptions
    {
        public Dictionary<Suppliers, string> SuppliersUrls { get; set; } = new Dictionary<Suppliers, string>();
    }
}