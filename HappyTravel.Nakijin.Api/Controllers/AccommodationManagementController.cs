using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.EdoContracts.Accommodations;
using HappyTravel.Nakijin.Api.Models.Mappers.Enums;
using HappyTravel.Nakijin.Api.Services;
using HappyTravel.Nakijin.Data.Models;
using HappyTravel.Nakijin.Api.Services.Workers;
using HappyTravel.Nakijin.Api.Services.Workers.AccommodationDataCalculation;
using HappyTravel.Nakijin.Api.Services.Workers.AccommodationMapping;
using HappyTravel.Nakijin.Data.Models.Accommodations;
using HappyTravel.SuppliersCatalog;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace HappyTravel.Nakijin.Api.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/{v:apiVersion}/[controller]")]
    [Produces("application/json")]
    [Authorize(Policy = "CanEdit")]
    public class AccommodationsManagementController : StaticDataControllerBase
    {
        public AccommodationsManagementController(IServiceProvider serviceProvider,
            IAccommodationManagementService accommodationManagementService)
        {
            _serviceProvider = serviceProvider;
            _accommodationManagementService = accommodationManagementService;
        }


        /// <summary>
        /// Cancels preloading
        /// </summary>
        /// <returns></returns>
        [HttpPost("preloading/cancel")]
        [ProducesResponseType((int) HttpStatusCode.OK)]
        public IActionResult CancelAccommodationPreloading()
        {
            _accommodationPreloaderTokenSource.Cancel();
            return Ok();
        }


        /// <summary>
        /// Cancels accommodation mapping
        /// </summary>
        /// <returns></returns>
        [HttpPost("mapping/cancel")]
        [ProducesResponseType((int) HttpStatusCode.OK)]
        public IActionResult CancelAccommodationMapping()
        {
            _accommodationMappingTokenSource.Cancel();
            return Ok();
        }


        /// <summary>
        /// Cancels accommodation merging 
        /// </summary>
        /// <returns></returns>
        [HttpPost("merging/cancel")]
        [ProducesResponseType((int) HttpStatusCode.OK)]
        public IActionResult CancelAccommodationDataMerge()
        {
            _accommodationDataMergeTokenSource.Cancel();
            return Ok();
        }


        /// <summary>
        /// Cancels accommodation data calculation
        /// </summary>
        /// <returns></returns>
        [HttpPost("calculation/cancel")]
        [ProducesResponseType((int) HttpStatusCode.OK)]
        public IActionResult CancelAccommodationDataCalculation()
        {
            _accommodationsDataCalculatorTokenSource.Cancel();
            return Ok();
        }


        /// <summary>
        /// Loads raw accommodation data 
        /// </summary>
        /// <param name="suppliers"></param>
        /// <returns></returns>
        [HttpPost("preloading/start")]
        [ProducesResponseType((int) HttpStatusCode.Accepted)]
        public IActionResult Preload([FromBody] List<Suppliers> suppliers)
        {
            // Prevent situation when done more than one Preload requests.
            if (_accommodationPreloaderTokenSource.Token.CanBeCanceled)
                _accommodationPreloaderTokenSource.Cancel();

            _accommodationPreloaderTokenSource = new CancellationTokenSource(TimeSpan.FromDays(1));
            var scope = _serviceProvider.CreateScope();

            Task.Factory.StartNew(async () =>
                {
                    try
                    {
                        var preloader = scope.ServiceProvider.GetRequiredService<IAccommodationPreloader>();
                        await preloader.Preload(suppliers, _accommodationPreloaderTokenSource.Token);
                    }
                    finally
                    {
                        scope.Dispose();
                    }
                },
                _accommodationPreloaderTokenSource.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
            return Accepted();
        }


        /// <summary>
        /// Maps accommodations 
        /// </summary>
        /// <param name="suppliers"></param>
        /// <returns></returns>
        [HttpPost("full-mapping/start")]
        [ProducesResponseType((int) HttpStatusCode.Accepted)]
        public IActionResult MapAccommodationsFully([FromBody] List<Suppliers> suppliers)
        {
            // Prevent situation when done more than one Map requests.
            if (_accommodationMappingTokenSource.Token.CanBeCanceled)
                _accommodationMappingTokenSource.Cancel();

            _accommodationMappingTokenSource = new CancellationTokenSource(TimeSpan.FromDays(1));
            var scope = _serviceProvider.CreateScope();

            Task.Factory.StartNew(async () =>
                {
                    try
                    {
                        var accommodationMapper = scope.ServiceProvider.GetRequiredService<IAccommodationMapper>();
                        await accommodationMapper.MapAccommodations(suppliers, MappingTypes.Full, _accommodationMappingTokenSource.Token);
                    }
                    finally
                    {
                        scope.Dispose();
                    }
                },
                _accommodationMappingTokenSource.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);

            return Accepted();
        }


        /// <summary>
        /// Maps accommodations 
        /// </summary>
        /// <param name="suppliers"></param>
        /// <returns></returns>
        [HttpPost("incremental-mapping/start")]
        [ProducesResponseType((int) HttpStatusCode.Accepted)]
        public IActionResult MapAccommodationsIncrementally([FromBody] List<Suppliers> suppliers)
        {
            // Prevent situation when done more than one Map requests.
            if (_accommodationMappingTokenSource.Token.CanBeCanceled)
                _accommodationMappingTokenSource.Cancel();

            _accommodationMappingTokenSource = new CancellationTokenSource(TimeSpan.FromDays(1));
            var scope = _serviceProvider.CreateScope();

            Task.Factory.StartNew(async () =>
                {
                    try
                    {
                        var accommodationMapper = scope.ServiceProvider.GetRequiredService<IAccommodationMapper>();
                        await accommodationMapper.MapAccommodations(suppliers, MappingTypes.Incremental, _accommodationMappingTokenSource.Token);
                    }
                    finally
                    {
                        scope.Dispose();
                    }
                },
                _accommodationMappingTokenSource.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);

            return Accepted();
        }


        /// <summary>
        /// Merges accommodations
        /// </summary>
        /// <returns></returns>
        [HttpPost("merging/start")]
        [ProducesResponseType((int) HttpStatusCode.Accepted)]
        public IActionResult MergeAccommodationsData()
        {
            // Prevent situation when done more than one Merge requests.
            if (_accommodationDataMergeTokenSource.Token.CanBeCanceled)
                _accommodationMappingTokenSource.Cancel();

            _accommodationDataMergeTokenSource = new CancellationTokenSource(TimeSpan.FromDays(1));
            var scope = _serviceProvider.CreateScope();

            Task.Factory.StartNew(async () =>
                {
                    try
                    {
                        var accommodationDataMerger = scope.ServiceProvider.GetRequiredService<IAccommodationDataMerger>();
                        await accommodationDataMerger.MergeAll(_accommodationDataMergeTokenSource.Token);
                    }
                    finally
                    {
                        scope.Dispose();
                    }
                },
                _accommodationDataMergeTokenSource.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);

            return Accepted();
        }


        /// <summary>
        /// Calculates accommodations data
        /// </summary>
        /// <returns></returns>
        [HttpPost("calculation/start")]
        [ProducesResponseType((int) HttpStatusCode.Accepted)]
        public IActionResult CalculateAccommodationsData([FromBody] List<Suppliers> suppliers)
        {
            // Prevent situation when done more than one Calculate requests.
            if (_accommodationsDataCalculatorTokenSource.Token.CanBeCanceled)
                _accommodationsDataCalculatorTokenSource.Cancel();

            _accommodationsDataCalculatorTokenSource = new CancellationTokenSource(TimeSpan.FromDays(1));
            var scope = _serviceProvider.CreateScope();

            Task.Factory.StartNew(async () =>
                {
                    try
                    {
                        var accommodationService = scope.ServiceProvider.GetRequiredService<IAccommodationDataMerger>();
                        await accommodationService.Calculate(suppliers, _accommodationsDataCalculatorTokenSource.Token);
                    }
                    finally
                    {
                        scope.Dispose();
                    }
                },
                _accommodationsDataCalculatorTokenSource.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);

            return Accepted();
        }


        /// <summary>
        /// Calculates accommodation final data
        /// </summary>
        /// <param name="id">Accommodation id</param>
        /// <returns></returns>
        [HttpPost("accommodations/{id}/recalculate")]
        [ProducesResponseType((int) HttpStatusCode.NoContent)]
        [ProducesResponseType((int) HttpStatusCode.BadRequest)]
        public async Task<IActionResult> RecalculateData(int id)
        {
            var (_, isFailure, error) = await _accommodationManagementService.RecalculateData(id);
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
        [HttpPost("accommodations/{accommodationId}/add-priorities")]
        [ProducesResponseType((int) HttpStatusCode.NoContent)]
        [ProducesResponseType((int) HttpStatusCode.BadRequest)]
        public async Task<IActionResult> AddSuppliersPriority(int accommodationId,
            [FromBody] Dictionary<AccommodationDataTypes, List<Suppliers>> suppliersPriorities)
        {
            var (_, isFailure, error) = await _accommodationManagementService.AddSuppliersPriority(accommodationId, suppliersPriorities);

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
        [HttpPost("accommodations/{accommodationId}/manual-correction")]
        [ProducesResponseType((int) HttpStatusCode.NoContent)]
        [ProducesResponseType((int) HttpStatusCode.BadRequest)]
        public async Task<IActionResult> AddManualCorrectionToAccommodation(int accommodationId,
            MultilingualAccommodation accommodation)
        {
            var (_, isFailure, error) =
                await _accommodationManagementService.AddManualCorrection(accommodationId, accommodation);

            if (isFailure)
                return BadRequest(error);

            return NoContent();
        }


        /// <summary>
        /// Matches uncertain matches
        /// </summary>
        /// <param name="uncertainMatchId"></param>
        /// <returns></returns>
        [HttpPost("accommodations/uncertain֊matches/{uncertainMatchId}/match")]
        [ProducesResponseType((int) HttpStatusCode.OK)]
        [ProducesResponseType((int) HttpStatusCode.BadRequest)]
        public async Task<IActionResult> MatchUncertain(int uncertainMatchId)
        {
            var (_, isFailure, error) = await _accommodationManagementService.MatchUncertain(uncertainMatchId);
            if (isFailure)
                return BadRequest(error);

            return Ok();
        }


        /// <summary>
        /// Matches two ht accommodations
        /// </summary>
        /// <param name="sourceHtId"></param>
        /// <param name="htIdToMatch"></param>
        /// <returns></returns>
        [HttpPost("accommodations/match")]
        [ProducesResponseType((int) HttpStatusCode.OK)]
        [ProducesResponseType((int) HttpStatusCode.BadRequest)]
        public async Task<IActionResult> MatchAccommodations(int sourceHtId, int htIdToMatch)
        {
            var (_, isFailure, error) =
                await _accommodationManagementService.MatchAccommodations(sourceHtId, htIdToMatch);
            if (isFailure)
                return BadRequest(error);

            return Ok();
        }


        /// <summary>
        /// Removes duplicate accommodations, which formed by supplier changed country codes.
        /// </summary>
        /// <param name="suppliers"></param>
        /// <returns></returns>
        [HttpPost("accommodations/duplicates/formed-by-supplier-country-change/remove")]
        [ProducesResponseType((int) HttpStatusCode.OK)]
        [ProducesResponseType((int) HttpStatusCode.BadRequest)]
        public async Task<IActionResult> RemoveDuplicatesFormedBySupplierChangedCountry(List<Suppliers> suppliers)
        {
            var (_, isFailure, error) = await _accommodationManagementService.RemoveDuplicatesFormedBySuppliersChangedCountry(suppliers);
            if (isFailure)
                return BadRequest(error);

            return Ok();
        }


        private static CancellationTokenSource _accommodationDataMergeTokenSource =
            new CancellationTokenSource(TimeSpan.FromDays(1));

        private static CancellationTokenSource _accommodationMappingTokenSource =
            new CancellationTokenSource(TimeSpan.FromDays(1));

        private static CancellationTokenSource _accommodationPreloaderTokenSource =
            new CancellationTokenSource(TimeSpan.FromDays(1));

        private static CancellationTokenSource _accommodationsDataCalculatorTokenSource =
            new CancellationTokenSource(TimeSpan.FromDays(1));


        private readonly IAccommodationManagementService _accommodationManagementService;
        private readonly IServiceProvider _serviceProvider;
    }
}