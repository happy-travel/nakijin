using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using HappyTravel.StaticDataMapper.Api.Services.Workers;
using HappyTravel.StaticDataMapper.Data.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace HappyTravel.StaticDataMapper.Api.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/{v:apiVersion}")]
    [Produces("application/json")]
    [Authorize(Policy = "CanEdit")]
    public class LocationsManagementController : StaticDataControllerBase
    {
        public LocationsManagementController(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        /// <summary>
        /// Maps locations (countries, localities, locality zones) of suppliers with existing locations.
        /// </summary>
        /// <param name="suppliers"></param>
        /// <returns></returns>
        [HttpPost("locations/map")]
        [ProducesResponseType((int) HttpStatusCode.Accepted)]
        public IActionResult MapLocations([FromBody] List<Suppliers> suppliers)
        {
            if (_locationsMapperTokenSource.Token.CanBeCanceled)
                _locationsMapperTokenSource.Cancel();

            _locationsMapperTokenSource = new CancellationTokenSource(TimeSpan.FromDays(1));
            Task.Run(async () =>
            {
                using var scope = _serviceProvider.CreateScope();

                var locationService = scope.ServiceProvider.GetRequiredService<ILocationMapper>();
                await locationService.MapLocations(suppliers);
            }, _locationsMapperTokenSource.Token);

            return Accepted();
        }


        private static CancellationTokenSource _locationsMapperTokenSource =
            new CancellationTokenSource(TimeSpan.FromDays(1));


        private readonly IServiceProvider _serviceProvider;
    }
}