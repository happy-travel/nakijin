using System;
using System.Threading;
using System.Threading.Tasks;

namespace HappyTravel.PropertyManagement.Api.Services.Workers
{
    public interface IAccommodationPreloader
    {
        Task Preload(DateTime? modificationDate = null, CancellationToken cancellationToken = default);
    }
}