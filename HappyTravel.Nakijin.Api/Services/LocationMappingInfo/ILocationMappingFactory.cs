using System.Collections.Generic;
using System.Threading.Tasks;
using HappyTravel.MapperContracts.Internal.Mappings;

namespace HappyTravel.Nakijin.Api.Services.LocationMappingInfo
{
    public interface ILocationMappingFactory
    {
        Task<List<LocationMapping>> GetForCountry(List<int> countryIds, string languageCode);
        Task<List<LocationMapping>> GetForLocality(List<int> localityIds, string languageCode);
        Task<List<LocationMapping>> GetForLocalityZone(List<int> localityZoneIds, string languageCode);
        Task<List<LocationMapping>> GetForAccommodation(List<int> accommodationIds, string languageCode);
    }
}