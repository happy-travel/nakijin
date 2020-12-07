using System.Threading.Tasks;
using HappyTravel.StaticDataMapper.Data.Models;

namespace HappyTravel.StaticDataMapper.Api.Infrastructure
{
    public interface ICountriesCache
    {
        Task Set(string countryCode, Country data);
        ValueTask<Country?> Get(string countryCode);
    }
}