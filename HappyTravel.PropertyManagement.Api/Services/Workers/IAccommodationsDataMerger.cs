using System.Threading;
using System.Threading.Tasks;
using HappyTravel.EdoContracts.Accommodations;
using HappyTravel.PropertyManagement.Data.Models.Accommodations;

namespace HappyTravel.PropertyManagement.Api.Services.Workers
{
    public interface IAccommodationsDataMerger
    {
        Task MergeAll(CancellationToken cancellationToken);

        Task<Accommodation> Merge(RichAccommodationDetails accommodation);
    }
}