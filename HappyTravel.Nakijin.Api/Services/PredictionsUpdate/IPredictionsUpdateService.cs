using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HappyTravel.Nakijin.Api.Models.LocationServiceInfo;
using HappyTravel.Nakijin.Api.Models.PredictionsUpdate;

namespace HappyTravel.Nakijin.Api.Services.PredictionsUpdate
{
    public interface IPredictionsUpdateService
    {
        Task Publish(Location location, EventTypes type);
        Task Publish(List<Location> locations, EventTypes type, CancellationToken cancellationToken = default);
    }
}