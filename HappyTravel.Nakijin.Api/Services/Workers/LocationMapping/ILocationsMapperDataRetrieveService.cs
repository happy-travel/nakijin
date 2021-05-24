using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HappyTravel.Nakijin.Data.Models;

namespace HappyTravel.Nakijin.Api.Services.Workers.LocationMapping
{
    public interface ILocationMapperDataRetrieveService
    {
        Task<List<Country>> GetNormalizedCountries();

        Task<List<Locality>> GetNormalizedLocalitiesByCountry(string countryCode, CancellationToken cancellationToken);


        Task<List<(string DefaultLocality, LocalityZone LocalityZone)>> GetNormalizedLocalityZonesByCountry(string countryCode,
            CancellationToken cancellationToken);


        Task<List<(string Code, int Id, string Name)>> GetCountries();
    }
}