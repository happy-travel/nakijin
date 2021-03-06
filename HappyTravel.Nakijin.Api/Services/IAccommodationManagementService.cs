using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.EdoContracts.Accommodations;
using HappyTravel.Nakijin.Data.Models;
using HappyTravel.Nakijin.Data.Models.Accommodations;
using HappyTravel.SuppliersCatalog;

namespace HappyTravel.Nakijin.Api.Services
{
    public interface IAccommodationManagementService
    {
        Task<Result> RecalculateData(int id);

        Task<Result> AddSuppliersPriority(int id, Dictionary<AccommodationDataTypes, List<Suppliers>> suppliersPriority);

        Task<Result> AddManualCorrection(int id, MultilingualAccommodation manualCorrectedAccommodation);

        Task<Result> MatchUncertain(int uncertainMatchId);

        Task<Result> MatchAccommodations(int sourceHtId, int htIdToMatch);

        Task<Result> RemoveDuplicatesFormedBySuppliersChangedCountry(List<Suppliers> suppliers);
    }
}