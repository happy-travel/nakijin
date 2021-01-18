using System.Threading;
using System.Threading.Tasks;
using HappyTravel.StaticDataMapper.Data.Models;

namespace HappyTravel.StaticDataMapper.Api.Services.Workers
{
    public interface ILocationMapper
    {
        Task MapLocations(Suppliers supplier, CancellationToken token = default);
        Task ConstructLocalitiesCache();
        Task ConstructCountriesCache();
        Task ConstructLocalityZonesCache();
    }
}