using System.Threading.Tasks;
using HappyTravel.PropertyManagement.Data.Models.Accommodations;
using NetTopologySuite.Index.Strtree;

namespace HappyTravel.PropertyManagement.Api.Infrastructure
{
    public interface IAccommodationsTreesCache
    {
        Task Set(string countryCode, STRtree<Accommodation> tree);
        ValueTask<STRtree<Accommodation>> Get(string countryCode);
    }
}