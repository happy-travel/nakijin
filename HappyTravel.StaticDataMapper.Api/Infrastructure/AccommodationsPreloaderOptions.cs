using System.Collections.Generic;
using HappyTravel.StaticDataMapper.Data.Models;

namespace HappyTravel.StaticDataMapper.Api.Infrastructure
{
    #nullable disable
    public class AccommodationsPreloaderOptions
    {
        public int BatchSize { get; set; }
        public List<Suppliers> Suppliers { get; set; }
    }
    #nullable restore
}
