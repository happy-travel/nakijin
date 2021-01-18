using System.Threading.Tasks;
using HappyTravel.StaticDataMapper.Api.Models.Mappers;
using HappyTravel.StaticDataMapper.Data.Models;

namespace HappyTravel.StaticDataMapper.Api.Infrastructure
{
    public interface ILocalitiesCache
    {
        Task Set(string countryCode, string localityName, Locality data);
        ValueTask<Locality?> Get(string countryCode, string localityName);
    }
}