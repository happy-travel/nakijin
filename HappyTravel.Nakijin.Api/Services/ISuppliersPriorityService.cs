using System.Collections.Generic;
using System.Threading.Tasks;
using HappyTravel.Nakijin.Data.Models;
using HappyTravel.Nakijin.Data.Models.Accommodations;
using HappyTravel.SuppliersCatalog;

namespace HappyTravel.Nakijin.Api.Services
{
    public interface ISuppliersPriorityService
    {
        ValueTask<Dictionary<AccommodationDataTypes, List<Suppliers>>> Get();

        Task AddOrUpdate(Dictionary<AccommodationDataTypes, List<Suppliers>> suppliersPriority);
    }
}