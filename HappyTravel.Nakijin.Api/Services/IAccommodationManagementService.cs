using System.Collections.Generic;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.EdoContracts.Accommodations;
using HappyTravel.Nakijin.Data.Models;
using HappyTravel.Nakijin.Data.Models.Accommodations;

namespace HappyTravel.Nakijin.Api.Services
{
    public interface IAccommodationManagementService
    {
        Task<Result> RecalculateData(int id);

        Task<Result> AddSuppliersPriority(int id, Dictionary<AccommodationDataTypes, List<Suppliers>> suppliersPriority);

        Task<Result> AddManualCorrection(int id, MultilingualAccommodation manualCorrectedAccommodation);

        Task<Result> MatchUncertain(int uncertainMatchId);
    }
}