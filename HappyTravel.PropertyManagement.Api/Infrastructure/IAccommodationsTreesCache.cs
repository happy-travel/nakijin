using System.Threading.Tasks;
using HappyTravel.PropertyManagement.Api.Models.Mappers;
using NetTopologySuite.Index.Strtree;

namespace HappyTravel.PropertyManagement.Api.Infrastructure
{
    public interface IAccommodationsTreesCache
    {
        Task Set(string countryCode, STRtree<AccommodationKeyData> tree);
        ValueTask<STRtree<AccommodationKeyData>> Get(string countryCode);
    }
}