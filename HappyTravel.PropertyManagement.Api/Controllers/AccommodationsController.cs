using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
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
        public AccommodationsController(IAccommodationPreloader preloader)
        {
            _preloader = preloader;
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
    
        
        private readonly IAccommodationPreloader _preloader;
    }
}
