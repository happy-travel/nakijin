using System;
using System.Collections.Generic;
using System.Linq;
using HappyTravel.Nakijin.Data.Models;
using HappyTravel.SuppliersCatalog;

namespace HappyTravel.Nakijin.Api.Infrastructure
{
    public static class ListStringExtensions
    {
        public static IEnumerable<Suppliers> ToSuppliersList(this List<string> suppliers)
        {
            if (!suppliers.Any()) return new List<Suppliers>();

            var result = new List<Suppliers>();

            foreach (var supplier in suppliers)
            {
                if (Enum.TryParse<Suppliers>(supplier, out var s))
                    result.Add(s);
            }

            return result;
        }
    }
}