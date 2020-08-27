using System.Threading;
using System.Threading.Tasks;
using HappyTravel.PropertyManagement.Api.Models.Mappers.Enums;

namespace HappyTravel.PropertyManagement.Api.Services.Mappers
{
    public interface IAccommodationMapper
    {
        Task MapSupplierAccommodations(Suppliers supplier, CancellationToken token);
    }
}