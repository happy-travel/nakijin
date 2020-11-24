using System.Collections.Generic;
using System.Threading.Tasks;
using HappyTravel.StaticDataMapper.Data.Models;
using HappyTravel.StaticDataMapper.Data.Models.Accommodations;

namespace HappyTravel.StaticDataMapper.Api.Services
{
    public interface ISuppliersPriorityService
    {
        ValueTask<Dictionary<AccommodationDataTypes, List<Suppliers>>> Get();
        Task AddOrUpdate(Dictionary<AccommodationDataTypes, List<Suppliers>> suppliersPriority);
    }
}