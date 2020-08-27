using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using HappyTravel.PropertyManagement.Api.Models.Mappers.Enums;
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


        [HttpPost("preload")]
        [ProducesResponseType((int) HttpStatusCode.OK)]
        public IActionResult Preload([FromQuery(Name = "modification-date")]
            DateTime? modificationDate, CancellationToken cancellationToken = default)
        {
            var source = new CancellationTokenSource(TimeSpan.FromDays(1));

            Task.Run(async () =>
            {
                using var scope = _serviceProvider.CreateScope();

                var preloader = scope.ServiceProvider.GetRequiredService<IAccommodationPreloader>();
                await preloader.Preload(modificationDate, source.Token);
            }, cancellationToken);
            return Accepted();
        }

        [HttpPost("map/suppliers/{supplier}")]
        public IActionResult MapSupplierAccommodations(Suppliers supplier)
        {
            // TODO: add cancellation token support
            Task.Run(async () =>
            {
                using var scope = _serviceProvider.CreateScope();

                var mapper = scope.ServiceProvider.GetRequiredService<IAccommodationMapper>();
                await mapper.MapSupplierAccommodations(supplier);
            });

            return Accepted();
        }

        private readonly IServiceProvider _serviceProvider;
    }
}