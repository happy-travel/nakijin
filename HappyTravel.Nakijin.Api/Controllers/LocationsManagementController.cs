using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Nakijin.Api.Infrastructure;
using HappyTravel.Nakijin.Api.Services;
using HappyTravel.Nakijin.Api.Services.Workers.LocationMapping;
using HappyTravel.SuppliersCatalog;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace HappyTravel.Nakijin.Api.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/{v:apiVersion}")]
    [Produces("application/json")]
    [Authorize(Policy = "CanEdit")]
    public class LocationsManagementController : StaticDataControllerBase
    {
        public LocationsManagementController(IServiceProvider serviceProvider, ILocationManagementService locationManagementService)
        {
            _serviceProvider = serviceProvider;
            _locationManagementService = locationManagementService;
        }


        /// <summary>
        /// Maps locations (countries, localities, locality zones) of suppliers with existing locations.
        /// </summary>
        /// <param name="suppliers"></param>
        /// <returns></returns>
        [HttpPost("locations/mapping/start")]
        [ProducesResponseType((int) HttpStatusCode.Accepted)]
        public IActionResult MapLocations([FromBody] List<Suppliers> suppliers)
        {
            if (_locationsMapperTokenSource.Token.CanBeCanceled)
                _locationsMapperTokenSource.Cancel();

            _locationsMapperTokenSource = new CancellationTokenSource(TimeSpan.FromDays(1));
            var scope = _serviceProvider.CreateScope();

            Task.Factory.StartNew(async () =>
                {
                    try
                    {
                        var locationMapper = scope.ServiceProvider.GetRequiredService<ILocationMapper>();
                        await locationMapper.MapLocations(suppliers, _locationsMapperTokenSource.Token);
                    }
                    finally
                    {
                        scope.Dispose();
                    }
                },
                _locationsMapperTokenSource.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);

            return Accepted();
        }

        
        /// <summary>
        /// Deactivates a locality and locality zones. LocalityId and LocalityZone fields in related accommodations will be set to NULL.
        /// </summary>
        [ProducesResponseType((int) HttpStatusCode.OK)]
        [ProducesResponseType((int) HttpStatusCode.BadRequest)]
        [HttpPost("locations/locality/deactivate/{localityHtId}")]
        public async Task<IActionResult> DeactivateLocality([FromRoute] string localityHtId, CancellationToken cancellationToken = default)
        {
            var (_, isFailure, error) = await _locationManagementService.Deactivate(localityHtId, cancellationToken);
            if (isFailure)
                return BadRequest(ProblemDetailsBuilder.Build(error));

            return Ok();
        }
        

        private static CancellationTokenSource _locationsMapperTokenSource =
            new CancellationTokenSource(TimeSpan.FromDays(1));


        private readonly IServiceProvider _serviceProvider;
        private readonly ILocationManagementService _locationManagementService;
    }
}