using System.Collections.Generic;
using System.Threading.Tasks;
using HappyTravel.StaticDataMapper.Api.Models.LocationInfo;

namespace HappyTravel.StaticDataMapper.Api.Services.LocationMappingInfo
{
    public interface ILocationMappingFactory
    {
        Task<List<LocationMapping>> GetForCountry(int[] ids, string languageCode);
        Task<List<LocationMapping>> GetForLocality(int[] ids, string languageCode);
        Task<List<LocationMapping>> GetForLocalityZone(int[] ids, string languageCode);
        Task<List<LocationMapping>> GetForAccommodation(int[] ids, string languageCode);
    }
}