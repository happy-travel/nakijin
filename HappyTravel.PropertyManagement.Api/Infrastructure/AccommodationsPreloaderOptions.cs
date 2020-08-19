using System.Collections.Generic;
using HappyTravel.PropertyManagement.Api.Models.Mappers.Enums;

namespace HappyTravel.PropertyManagement.Api.Infrastructure
{
    #nullable disable
    public class AccommodationsPreloaderOptions
    {
        public int BatchSize { get; set; }
        public List<Suppliers> Suppliers { get; set; }
    }
    #nullable restore
}
