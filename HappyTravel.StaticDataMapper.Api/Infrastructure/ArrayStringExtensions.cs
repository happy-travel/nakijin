using System;
using System.Collections.Generic;
using HappyTravel.StaticDataMapper.Data.Models;

namespace HappyTravel.StaticDataMapper.Api.Infrastructure
{
    public static class ArrayStringExtensions
    {
        public static List<Suppliers> ToSuppliersList(this string[]? suppliers)
        {
            if (suppliers == null) return null;

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