using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.StaticDataMapper.Api.Models.LocationInfo;

namespace HappyTravel.StaticDataMapper.Api.Services.LocationMappingInfo
{
    public interface ILocationMappingInfoService
    {
        Task<Result<LocationMapping>> Get(string htId);
    }
}