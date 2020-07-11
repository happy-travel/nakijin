using System.Collections.Generic;

namespace HappyTravel.PropertyManagement.Api.Infrastructure
{
    #nullable disable
    public class AccommodationsPreloaderOptions
    {
        public int BatchSize { get; set; }
        public List<string> Suppliers { get; set; }
    }
    #nullable restore
}
