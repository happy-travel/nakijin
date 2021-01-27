using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using HappyTravel.StaticDataMapper.Api.Filters.Authorization;
using HappyTravel.StaticDataMapper.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Country = LocationNameNormalizer.Models.Country;

namespace HappyTravel.StaticDataMapper.Api.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/{v:apiVersion}/[controller]")]
    [Produces("application/json")]
    [Permissions(MapperPermissions.Read)]
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
        public async Task<IActionResult> GetCountries()
        {
            var countries = await _locationService.GetCountries(LanguageCode);
            return Ok(countries);
        }
        
        
        private readonly ILocationService _locationService;
    }
}