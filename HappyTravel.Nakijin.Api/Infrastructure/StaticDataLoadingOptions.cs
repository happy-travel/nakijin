using System.Collections.Generic;
using HappyTravel.Nakijin.Data.Models;

namespace HappyTravel.Nakijin.Api.Infrastructure
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