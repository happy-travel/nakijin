using System;
using System.Threading;
using System.Threading.Tasks;

namespace HappyTravel.StaticDataMapper.Api.Services.Workers
{
    public interface IAccommodationPreloader
    {
        Task Preload(DateTime? modificationDate = null, CancellationToken cancellationToken = default);
    }
}