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
        ///  Replaces the id of removable location by a substitution location id. Accommodations must have the relation with the new location and a new zone (if substitutionalZoneHtId specified) instead of the removable location.
        ///  After this calculation/start Endpoint must be  
        /// </summary>
        /// <param name="htIdToRemove">A location id to remove </param>
        /// <param name="substitutionalHtId">A location id for substituting the removable location</param>
        /// <param name="substitutionalZoneHtId">An id for substituting zone id for accommodations associated with the removable location</param>
        /// <returns></returns>
        [ProducesResponseType((int) HttpStatusCode.OK)]
        [ProducesResponseType((int) HttpStatusCode.BadRequest)]
        [HttpDelete("locations/locality/remove/{htIdToRemove}/substitute/{substitutionalHtId}")]
        [HttpDelete("locations/locality/remove/{htIdToRemove}/substitute/{substitutionalHtId}/zone/{substitutionalZoneHtId}")]
        public async Task<IActionResult> RemoveLocalityWithSubstitution([FromRoute] string htIdToRemove, [FromRoute] string substitutionalHtId, [FromRoute] string substitutionalZoneHtId = null!)
        {
            var (_, isFailure, error) = await _locationManagementService.RemoveLocality(htIdToRemove, substitutionalHtId, substitutionalZoneHtId);
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