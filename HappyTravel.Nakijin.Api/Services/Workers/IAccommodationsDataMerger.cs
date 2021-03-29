using System.Threading;
using System.Threading.Tasks;
using HappyTravel.EdoContracts.Accommodations;
using HappyTravel.Nakijin.Data.Models.Accommodations;

namespace HappyTravel.Nakijin.Api.Services.Workers
{
    public interface IAccommodationsDataMerger
    {
        Task MergeAll(CancellationToken cancellationToken);

        Task<MultilingualAccommodation> Merge(RichAccommodationDetails accommodation);
    }
}