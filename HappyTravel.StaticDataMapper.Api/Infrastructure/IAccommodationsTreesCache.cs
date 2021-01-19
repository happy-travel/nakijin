using System.Threading.Tasks;
using HappyTravel.StaticDataMapper.Api.Models.Mappers;
using NetTopologySuite.Index.Strtree;

namespace HappyTravel.StaticDataMapper.Api.Infrastructure
{
    public interface IAccommodationsTreesCache
    {
        Task Set(string countryCode, STRtree<AccommodationKeyData> tree);
        ValueTask<STRtree<AccommodationKeyData>> Get(string countryCode);
        Task Remove(string countryCode);
    }
}