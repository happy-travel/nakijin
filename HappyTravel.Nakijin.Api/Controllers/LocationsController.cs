using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using HappyTravel.Nakijin.Api.Infrastructure;
using HappyTravel.Nakijin.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HappyTravel.MapperContracts.Public.Locations;

namespace HappyTravel.Nakijin.Api.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/{v:apiVersion}/[controller]")]
    [Produces("application/json")]
    [Authorize]
    public class LocationsController : StaticDataControllerBase
    {
        public LocationsController(ILocationService locationService)
        {
            _locationService = locationService;
        }
        
        
        /// <summary>
        /// Gets all available countries list
        /// </summary>
        /// <returns></returns>
        [HttpGet("countries")]
        [ProducesResponseType(typeof(List<Country>), (int) HttpStatusCode.OK)]
        public async Task<IActionResult> GetCountries([FromQuery] List<string> suppliers)
        {
            var countries = await _locationService.GetCountries(suppliers.ToSuppliersList(), LanguageCode);
            return Ok(countries);
        }
        
        /// <summary>
        ///  Gets date of last modified location.
        /// </summary>
        /// <returns>Last changed location modified date</returns>
        [ProducesResponseType(typeof(DateTime), (int) HttpStatusCode.OK)]
        [HttpGet("last-modified-date")]
        public async Task<IActionResult> GetLastModifiedDate()
        {
            var lastModifiedDate = await _locationService.GetLastModifiedDate();

            return Ok(lastModifiedDate);
        }
        
        
        private readonly ILocationService _locationService;
    }
}