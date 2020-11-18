using System.Threading;
using System.Threading.Tasks;
using HappyTravel.EdoContracts.Accommodations;
using HappyTravel.PropertyManagement.Data.Models.Accommodations;

namespace HappyTravel.PropertyManagement.Api.Services.Workers
{
    public interface IAccommodationsDataMerger
    {
        Task MergeAccommodationsData(CancellationToken cancellationToken);

        Task<Accommodation> MergeData(RichAccommodationDetails accommodation);
    }
}