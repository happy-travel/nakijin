using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using HappyTravel.PropertyManagement.Data.Models;
using HappyTravel.PropertyManagement.Api.Services.Mappers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace HappyTravel.PropertyManagement.Api.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/{v:apiVersion}/[controller]")]
    [Produces("application/json")]
    public class AccommodationsController : ControllerBase
    {
        public AccommodationsController(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
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

        private readonly IServiceProvider _serviceProvider;
    }
}