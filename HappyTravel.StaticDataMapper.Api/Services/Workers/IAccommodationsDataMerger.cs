using System.Threading;
using System.Threading.Tasks;
using HappyTravel.EdoContracts.Accommodations;
using HappyTravel.StaticDataMapper.Data.Models.Accommodations;

namespace HappyTravel.StaticDataMapper.Api.Services.Workers
{
    public interface IAccommodationsDataMerger
    {
        Task MergeAll(CancellationToken cancellationToken);

        Task<MultilingualAccommodation> Merge(RichAccommodationDetails accommodation);
    }
}