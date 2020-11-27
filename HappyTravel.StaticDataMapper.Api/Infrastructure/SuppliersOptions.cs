using System.Collections.Generic;
using HappyTravel.StaticDataMapper.Data.Models;

namespace HappyTravel.StaticDataMapper.Api.Infrastructure
{
    public class SuppliersOptions
    {
        public Dictionary<Suppliers, string> SuppliersUrls { get; set; } = new Dictionary<Suppliers, string>();
    }
}