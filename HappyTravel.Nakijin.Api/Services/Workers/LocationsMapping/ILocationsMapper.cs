using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HappyTravel.Nakijin.Data.Models;

namespace HappyTravel.Nakijin.Api.Services.Workers.LocationsMapping
{
    public interface ILocationsMapper
    {
        Task MapLocations(List<Suppliers> suppliers, CancellationToken token = default);
    }
}