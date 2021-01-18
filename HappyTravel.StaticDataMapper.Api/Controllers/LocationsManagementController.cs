using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using HappyTravel.StaticDataMapper.Api.Services.Workers;
using HappyTravel.StaticDataMapper.Data.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace HappyTravel.StaticDataMapper.Api.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/{v:apiVersion}")]
    [Produces("application/json")]
    public class LocationsManagementController : ControllerBase
    {
        public LocationsManagementController(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        [HttpPost("locations/map/suppliers/{supplier}")]
        [ProducesResponseType((int) HttpStatusCode.Accepted)]
        public IActionResult MapLocations([FromRoute] Suppliers supplier)
        {
            if (_locationsMapperTokenSource.Token.CanBeCanceled)
                _locationsMapperTokenSource.Cancel();

            _locationsMapperTokenSource = new CancellationTokenSource(TimeSpan.FromDays(1));
            Task.Run(async () =>
            {
                using var scope = _serviceProvider.CreateScope();

                var locationService = scope.ServiceProvider.GetRequiredService<ILocationMapper>();
                await locationService.MapLocations(supplier);
            }, _locationsMapperTokenSource.Token);

            return Accepted();
        }
        
        private static CancellationTokenSource _locationsMapperTokenSource =
            new CancellationTokenSource(TimeSpan.FromDays(1));


        private readonly IServiceProvider _serviceProvider;
    }
}