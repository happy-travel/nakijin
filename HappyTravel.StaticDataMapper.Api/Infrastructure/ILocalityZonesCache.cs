using System.Threading.Tasks;
using HappyTravel.StaticDataMapper.Data.Models;

namespace HappyTravel.StaticDataMapper.Api.Infrastructure
{
    public interface ILocalityZonesCache
    {
        Task Set(string countryCode, string localityName,string localityZoneName, LocalityZone data);
        ValueTask<LocalityZone?> Get(string countryCode, string localityName, string localityZoneName);
    }
}