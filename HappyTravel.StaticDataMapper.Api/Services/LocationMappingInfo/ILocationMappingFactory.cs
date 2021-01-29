using System.Collections.Generic;
using System.Threading.Tasks;
using HappyTravel.StaticDataMapper.Api.Models.LocationInfo;

namespace HappyTravel.StaticDataMapper.Api.Services.LocationMappingInfo
{
    public interface ILocationMappingFactory
    {
        Task<List<LocationMapping>> GetForCountry(List<int> countryIds, string languageCode);
        Task<List<LocationMapping>> GetForLocality(List<int> localityIds, string languageCode);
        Task<List<LocationMapping>> GetForLocalityZone(List<int> localityZoneIds, string languageCode);
        Task<List<LocationMapping>> GetForAccommodation(List<int> accommodationIds, string languageCode);
    }
}