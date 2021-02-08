using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.StaticDataMapper.Api.Models.LocationInfo;
using HappyTravel.StaticDataMapper.Api.Models.LocationServiceInfo;
using HappyTravel.StaticDataMapper.Api.Services.LocationMappingInfo;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Location = HappyTravel.StaticDataMapper.Api.Models.LocationInfo.Location;

namespace HappyTravel.StaticDataMapper.Api.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/{v:apiVersion}/location-mappings")]
    [Produces("application/json")]
    [Authorize]
    public class LocationMappingsController : StaticDataControllerBase
    {
        public LocationMappingsController(ILocationMappingInfoService locationMappingInfoService)
        {
            _locationMappingInfoService = locationMappingInfoService;
        }
        
        
        /// <summary>
        /// Gets location mapping list info by given htId list
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [ProducesResponseType(typeof(List<LocationMapping>), (int) HttpStatusCode.OK)]
        public async Task<IActionResult> GetMapping([FromQuery] List<string> htIds)
        {
            var (_, isFailure, locationMappings, error) = await _locationMappingInfoService.Get(htIds, LanguageCode);

            if (isFailure)
                return BadRequest(error);

            return Ok(locationMappings);
        }
        

        /// <summary>
        /// Retrieves locations by a location type
        /// </summary>
        /// <param name="locationType">Type of location</param>
        /// <param name="modified">Since which date locations will be retrieved</param>
        /// <param name="skip">Skip locations</param>
        /// <param name="top">Take locations</param>
        /// <param name="cancellationToken"></param>
        /// <returns>List of locations</returns>
        [HttpGet("locations")]
        [ProducesResponseType(typeof(List<Location>), (int) HttpStatusCode.OK)]
        public async Task<IActionResult> GetLocations([FromQuery] AccommodationMapperLocationTypes locationType, [FromQuery] DateTime modified = default, [FromQuery] int skip = 0, [Range(0, 50000)][FromQuery] int top = 50000, CancellationToken cancellationToken = default)
            => Ok(await _locationMappingInfoService.Get(locationType, LanguageCode, modified, skip, top, cancellationToken));
        
        
        private readonly ILocationMappingInfoService _locationMappingInfoService;
    }
}