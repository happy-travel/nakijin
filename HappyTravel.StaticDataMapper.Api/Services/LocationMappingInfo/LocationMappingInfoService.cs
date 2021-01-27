using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.StaticDataMapper.Api.Models.LocationInfo;

namespace HappyTravel.StaticDataMapper.Api.Services.LocationMappingInfo
{
    public class LocationMappingInfoService : ILocationMappingInfoService
    {
        public async Task<Result<LocationMapping>> Get(string htId)
        {
            // TODO add htId parsing + getting location info
            return new LocationMapping();
        }
    }
}