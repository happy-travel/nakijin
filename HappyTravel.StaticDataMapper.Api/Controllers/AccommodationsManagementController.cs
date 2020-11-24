using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using HappyTravel.StaticDataMapper.Data.Models;
using HappyTravel.StaticDataMapper.Api.Services.Workers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace HappyTravel.StaticDataMapper.Api.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/{v:apiVersion}/[controller]")]
    [Produces("application/json")]
    public class AccommodationsManagementController : ControllerBase
    {
        public AccommodationsManagementController(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
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


        [HttpPost("merge/cancel")]
        [ProducesResponseType((int) HttpStatusCode.OK)]
        public IActionResult CancelAccommodationDataMerge()
        {
            _accommodationDataMergeTokenSource.Cancel();
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


        [HttpPost("merge")]
        [ProducesResponseType((int) HttpStatusCode.Accepted)]
        public IActionResult MergeAccommodationsData()
        {
            // Prevent situation when done more than one Merge requests.
            if (_accommodationDataMergeTokenSource.Token.CanBeCanceled)
                _accommodationMappingTokenSource.Cancel();

            _accommodationDataMergeTokenSource = new CancellationTokenSource(TimeSpan.FromDays(1));

            Task.Run(async () =>
            {
                using var scope = _serviceProvider.CreateScope();

                var accommodationService = scope.ServiceProvider.GetRequiredService<IAccommodationsDataMerger>();
                await accommodationService.MergeAll(_accommodationDataMergeTokenSource.Token);
            }, _accommodationDataMergeTokenSource.Token);

            return Accepted();
        }


        private static CancellationTokenSource _accommodationDataMergeTokenSource =
            new CancellationTokenSource(TimeSpan.FromDays(1));

        private static CancellationTokenSource _accommodationMappingTokenSource =
            new CancellationTokenSource(TimeSpan.FromDays(1));

        private static CancellationTokenSource _accommodationPreloaderTokenSource =
            new CancellationTokenSource(TimeSpan.FromDays(1));

        private readonly IServiceProvider _serviceProvider;
    }
}