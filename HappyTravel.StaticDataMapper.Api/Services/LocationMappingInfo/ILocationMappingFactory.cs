using System.Threading.Tasks;
using HappyTravel.StaticDataMapper.Api.Models.LocationInfo;

namespace HappyTravel.StaticDataMapper.Api.Services.LocationMappingInfo
{
    public interface ILocationMappingFactory
    {
        Task<LocationMapping> GetForCountry(int id, string languageCode);
        Task<LocationMapping> GetForLocality(int id, string languageCode);
        Task<LocationMapping> GetForLocalityZone(int id, string languageCode);
        Task<LocationMapping> GetForAccommodation(int id, string languageCode);
    }
}