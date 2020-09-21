using System.Threading;
using System.Threading.Tasks;
using HappyTravel.PropertyManagement.Data.Models;

namespace HappyTravel.PropertyManagement.Api.Services.Mappers
{
    public interface IAccommodationMapper
    {
        Task MapAccommodations(Suppliers supplier, CancellationToken token);
    }
}