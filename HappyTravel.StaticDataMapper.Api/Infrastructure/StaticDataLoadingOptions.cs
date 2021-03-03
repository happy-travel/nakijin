using System.Collections.Generic;
using HappyTravel.StaticDataMapper.Data.Models;

namespace HappyTravel.StaticDataMapper.Api.Infrastructure
{
#nullable disable
    public class StaticDataLoadingOptions
    {
        public int PreloadingBatchSize { get; set; }
        public int MappingBatchSize { get; set; }
        public int MergingBatchSize { get; set; }
        public int DbCommandTimeOut { get; set; }
    }
#nullable restore
}