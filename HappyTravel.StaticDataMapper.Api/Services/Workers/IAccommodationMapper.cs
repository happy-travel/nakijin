using System.Threading;
using System.Threading.Tasks;
using HappyTravel.StaticDataMapper.Data.Models;

namespace HappyTravel.StaticDataMapper.Api.Services.Workers
{
    public interface IAccommodationMapper
    {
        Task MapAccommodations(Suppliers supplier, CancellationToken token);
    }
}