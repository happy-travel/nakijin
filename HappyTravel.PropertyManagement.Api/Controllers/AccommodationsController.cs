using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using HappyTravel.PropertyManagement.Api.Models.Mappers.Enums;
using HappyTravel.PropertyManagement.Api.Services.Mappers;
using Microsoft.AspNetCore.Mvc;

namespace HappyTravel.PropertyManagement.Api.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/{v:apiVersion}/[controller]")]
    [Produces("application/json")]
    public class AccommodationsController : ControllerBase
    {
        public AccommodationsController(IAccommodationPreloader preloader, IAccommodationMapper mapper)
        {
            _preloader = preloader;
            _mapper = mapper;
        }


        [HttpGet]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        public async Task<IActionResult> Get()
        {
            return await Task.FromResult(Ok());
        }


        [HttpPost("preload")]
        [ProducesResponseType((int) HttpStatusCode.OK)]
        public async Task<IActionResult> Preload([FromQuery(Name = "modification-date")] DateTime? modificationDate, CancellationToken cancellationToken = default)
        {
            var source = new CancellationTokenSource(TimeSpan.FromDays(1));
            await _preloader.Preload(modificationDate, source.Token);
            
            return Accepted();
        }

        [HttpPost("map/suppliers/{supplier}")]
        public async Task<IActionResult> MapSupplierAccommodations(Suppliers supplier)
        {
            await _mapper.MapSupplierAccommodations(supplier);
            return Accepted();
        }

        private readonly IAccommodationMapper _mapper;
        private readonly IAccommodationPreloader _preloader;
    }
}
