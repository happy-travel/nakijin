using System.Collections.Generic;
using System.Threading.Tasks;
using HappyTravel.PropertyManagement.Data.Models;
using HappyTravel.PropertyManagement.Data.Models.Accommodations;

namespace HappyTravel.PropertyManagement.Api.Services
{
    public interface ISuppliersPriorityService
    {
        ValueTask<Dictionary<AccommodationDataTypes, List<Suppliers>>> Get();
        Task AddOrUpdate(Dictionary<AccommodationDataTypes, List<Suppliers>> suppliersPriority);
    }
}