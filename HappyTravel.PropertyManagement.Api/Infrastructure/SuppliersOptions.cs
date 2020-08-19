using System.Collections.Generic;
using HappyTravel.PropertyManagement.Api.Models.Mappers.Enums;

namespace HappyTravel.PropertyManagement.Api.Infrastructure
{
    public class SuppliersOptions
    {
        public SuppliersOptions()
        {
            SuppliersUrls = new Dictionary<Suppliers, string>();
        }
        public Dictionary<Suppliers, string> SuppliersUrls { get; set; }
    }
}