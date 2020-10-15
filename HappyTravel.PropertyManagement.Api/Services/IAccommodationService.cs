using System.Collections.Generic;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.EdoContracts.Accommodations;
using HappyTravel.PropertyManagement.Data.Models;
using HappyTravel.PropertyManagement.Data.Models.Accommodations;

namespace HappyTravel.PropertyManagement.Api.Services
{
    public interface IAccommodationService
    {
        Task<Result> RecalculateAccommodationData(int id);
        Task<Result> AddSuppliersPriorityToAccommodation(int id, Dictionary<AccommodationDataTypes, List<Suppliers>> suppliersPriority);
        Task<Accommodation> MergeAccommodationsData(WideAccommodationDetails wideAccommodationDetails, Dictionary<Suppliers, AccommodationDetails> supplierAccommodationDetails);
        Task<Result> AddManualCorrectionToAccommodation(int id, Accommodation manualCorrectedAccommodation);
    }
}