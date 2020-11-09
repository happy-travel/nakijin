using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.EdoContracts.Accommodations;
using HappyTravel.PropertyManagement.Data.Models;
using HappyTravel.PropertyManagement.Data.Models.Accommodations;

namespace HappyTravel.PropertyManagement.Api.Services
{
    public interface IAccommodationService
    {
        Task<Result> RecalculateData(int id);

        Task<Result> AddSuppliersPriority(int id, Dictionary<AccommodationDataTypes, List<Suppliers>> suppliersPriority);

        Task<Result> AddManualCorrection(int id, Accommodation manualCorrectedAccommodation);

        Task MergeAccommodationsData(CancellationToken cancellationToken);
    }
}