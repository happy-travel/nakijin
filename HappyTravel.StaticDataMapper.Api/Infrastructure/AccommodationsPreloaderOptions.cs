using System.Collections.Generic;
using HappyTravel.StaticDataMapper.Data.Models;

namespace HappyTravel.StaticDataMapper.Api.Infrastructure
{
#nullable disable
    public class StaticDataLoadingOptions
    {
        public int BatchSize { get; set; }
        public int DbCommandTimeOut { get; set; }
    }
#nullable restore
}