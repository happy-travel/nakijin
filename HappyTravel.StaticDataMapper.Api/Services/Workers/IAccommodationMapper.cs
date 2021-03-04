using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HappyTravel.StaticDataMapper.Data.Models;

namespace HappyTravel.StaticDataMapper.Api.Services.Workers
{
    public interface IAccommodationMapper
    {
        Task MapAccommodations(List<Suppliers> suppliers, CancellationToken token);
    }
}