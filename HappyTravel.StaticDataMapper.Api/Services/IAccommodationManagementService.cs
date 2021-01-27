using System.Collections.Generic;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.EdoContracts.Accommodations;
using HappyTravel.StaticDataMapper.Data.Models;
using HappyTravel.StaticDataMapper.Data.Models.Accommodations;

namespace HappyTravel.StaticDataMapper.Api.Services
{
    public interface IAccommodationManagementService
    {
        Task<Result> RecalculateData(int id);

        Task<Result> AddSuppliersPriority(int id, Dictionary<AccommodationDataTypes, List<Suppliers>> suppliersPriority);

        Task<Result> AddManualCorrection(int id, MultilingualAccommodation manualCorrectedAccommodation);

        Task<Result> MatchUncertain(int uncertainMatchId);
    }
}