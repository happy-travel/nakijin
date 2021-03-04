using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HappyTravel.StaticDataMapper.Data.Models;

namespace HappyTravel.StaticDataMapper.Api.Services.Workers
{
    public interface ILocationMapper
    {
        Task MapLocations(List<Suppliers> suppliers, CancellationToken token = default);
    }
}