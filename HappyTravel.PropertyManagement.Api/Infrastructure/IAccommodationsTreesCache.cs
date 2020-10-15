using System.Collections.Generic;
using System.Threading.Tasks;
using HappyTravel.PropertyManagement.Data.Models.Accommodations;
using NetTopologySuite.Index.Strtree;

namespace HappyTravel.PropertyManagement.Api.Infrastructure
{
    public interface IAccommodationsTreesCache
    {
        Task Set(string countryCode, STRtree<KeyValuePair<int,Accommodation>> tree);
        ValueTask<STRtree<KeyValuePair<int,Accommodation>>> Get(string countryCode);
    }
}