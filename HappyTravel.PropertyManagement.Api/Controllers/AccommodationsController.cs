using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.PropertyManagement.Api.Services;
using HappyTravel.PropertyManagement.Data.Models;
using HappyTravel.PropertyManagement.Api.Services.Mappers;
using HappyTravel.PropertyManagement.Data.Models.Accommodations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Accommodation = HappyTravel.EdoContracts.Accommodations.Accommodation;

namespace HappyTravel.PropertyManagement.Api.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/{v:apiVersion}/[controller]")]
    [Produces("application/json")]
    public class AccommodationsController : ControllerBase
    {
        public AccommodationsController(IServiceProvider serviceProvider, IAccommodationService accommodationService)
        {
            _serviceProvider = serviceProvider;
            _accommodationService = accommodationService;
        }

        /// <summary>
        /// Calculates accommodation final data
        /// </summary>
        /// <param name="id">Accommodation id</param>
        /// <returns></returns>
        [HttpPost("{id}/recalculate")]
        [ProducesResponseType((int) HttpStatusCode.NoContent)]
        [ProducesResponseType((int) HttpStatusCode.BadRequest)]
        public async Task<IActionResult> RecalculateData(int id)
        {
            var (_, isFailure, error) = await _accommodationService.RecalculateData(id);
            if (isFailure)
                return BadRequest(error);

            return NoContent();
        }

        /// <summary>
        /// Adds suppliers priority to accommodation
        /// </summary>
        /// <param name="accommodationId"></param>
        /// <param name="suppliersPriorities"></param>
        /// <returns></returns>
        [HttpPost("{accommodationId}/add-priorities")]
        [ProducesResponseType((int) HttpStatusCode.NoContent)]
        [ProducesResponseType((int) HttpStatusCode.BadRequest)]
        public async Task<IActionResult> AddSuppliersPriority(int accommodationId, [FromBody]Dictionary<AccommodationDataTypes, List<Suppliers>> suppliersPriorities)
        {
            var (_, isFailure, error) =
                await _accommodationService.AddSuppliersPriority(accommodationId, suppliersPriorities);

            if (isFailure)
                return BadRequest(error);

            return NoContent();
        }

        /// <summary>
        ///  Adds manual correction to accommodation
        /// </summary>
        /// <param name="accommodationId"></param>
        /// <param name="accommodation"></param>
        /// <returns></returns>
        [HttpPost("{accommodationId}/manual-correction")]
        [ProducesResponseType((int) HttpStatusCode.NoContent)]
        [ProducesResponseType((int) HttpStatusCode.BadRequest)]
        public async Task<IActionResult> AddManualCorrectionToAccommodation(int accommodationId, Accommodation accommodation)
        {
            var (_, isFailure, error) =
                await _accommodationService.AddManualCorrection(accommodationId, accommodation);

            if (isFailure)
                return BadRequest(error);

            return NoContent();
        }

        [HttpGet]
        [ProducesResponseType((int) HttpStatusCode.OK)]
        public async Task<IActionResult> Get()
        {
            return await Task.FromResult(Ok());
        }

        [HttpPost("preload/cancel")]
        [ProducesResponseType((int) HttpStatusCode.OK)]
        public IActionResult CancelAccommodationPreloading()
        {
            _accommodationPreloaderTokenSource.Cancel();
            return Ok();
        }

        [HttpPost("preload")]
        [ProducesResponseType((int) HttpStatusCode.Accepted)]
        public IActionResult Preload([FromQuery(Name = "modification-date")]
            DateTime? modificationDate, CancellationToken cancellationToken = default)
        {
            // Prevent situation when done more than one Preload requests.
            if (_accommodationPreloaderTokenSource.Token.CanBeCanceled)
                _accommodationPreloaderTokenSource.Cancel();

            _accommodationPreloaderTokenSource = new CancellationTokenSource(TimeSpan.FromDays(1));

            Task.Run(async () =>
            {
                using var scope = _serviceProvider.CreateScope();

                var preloader = scope.ServiceProvider.GetRequiredService<IAccommodationPreloader>();
                await preloader.Preload(modificationDate, _accommodationPreloaderTokenSource.Token);
            }, cancellationToken);
            return Accepted();
        }

        [HttpPost("map/cancel")]
        [ProducesResponseType((int) HttpStatusCode.OK)]
        public IActionResult CancelAccommodationMapping()
        {
            _accommodationMappingTokenSource.Cancel();
            return Ok();
        }

        [HttpPost("map/suppliers/{supplier}")]
        [ProducesResponseType((int) HttpStatusCode.Accepted)]
        public IActionResult MapAccommodations(Suppliers supplier)
        {
            // Prevent situation when done more than one Map requests.
            if (_accommodationMappingTokenSource.Token.CanBeCanceled)
                _accommodationMappingTokenSource.Cancel();

            _accommodationMappingTokenSource = new CancellationTokenSource(TimeSpan.FromDays(1));

            Task.Run(async () =>
            {
                using var scope = _serviceProvider.CreateScope();

                var mapper = scope.ServiceProvider.GetRequiredService<IAccommodationMapper>();
                await mapper.MapAccommodations(supplier, _accommodationMappingTokenSource.Token);
            }, _accommodationMappingTokenSource.Token);

            return Accepted();
        }

        private static CancellationTokenSource _accommodationMappingTokenSource =
            new CancellationTokenSource(TimeSpan.FromDays(1));

        private static CancellationTokenSource _accommodationPreloaderTokenSource =
            new CancellationTokenSource(TimeSpan.FromDays(1));

        private readonly IAccommodationService _accommodationService;
        private readonly IServiceProvider _serviceProvider;
    }
}