using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HappyTravel.Nakijin.Api.Models.LocationServiceInfo;
using HappyTravel.Nakijin.Api.Models.StaticDataPublications;

namespace HappyTravel.Nakijin.Api.Services.StaticDataPublication
{
    public interface IStaticDataPublicationService
    {
        Task Publish(Location location, UpdateEventTypes type);
        
        Task Publish(List<Location> locations, UpdateEventTypes type, CancellationToken cancellationToken = default);
    }
}