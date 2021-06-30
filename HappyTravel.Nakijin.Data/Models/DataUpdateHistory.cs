using System;
using HappyTravel.SuppliersCatalog;

namespace HappyTravel.Nakijin.Data.Models
{
    public class DataUpdateHistory
    {
        public int Id { get; set; }
        public DataUpdateTypes Type { get; set; }
        public Suppliers Supplier { get; set; }
        public DateTime UpdateTime { get; set; }
    }
}