using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using HappyTravel.StaticDataMapper.Api.Filters.Authorization;
using HappyTravel.StaticDataMapper.Api.Models.Locations;
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

        
        /// <summary>
        /// Retrieves locations by a location type 
        /// </summary>
        /// <param name="locationType"></param>
        /// <param name="modified"></param>
        /// <param name="skip"></param>
        /// <param name="top"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>List of locations</returns>
        [HttpGet]
        [ProducesResponseType(typeof(List<Location>), (int) HttpStatusCode.OK)]
        public async Task<IActionResult> GetLocations([FromQuery] AccommodationMapperLocationTypes locationType, [FromQuery] DateTime modified = default, [FromQuery] int skip = 0, [Range(0, 50000)][FromQuery] int top = 50000, CancellationToken cancellationToken = default)
            => Ok(await _locationService.Get(locationType, LanguageCode, modified, skip, top, cancellationToken));
        
        
        private readonly ILocationService _locationService;

    }
}