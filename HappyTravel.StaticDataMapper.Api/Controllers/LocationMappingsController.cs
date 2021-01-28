using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.StaticDataMapper.Api.Filters.Authorization;
using HappyTravel.StaticDataMapper.Api.Models.LocationInfo;
using HappyTravel.StaticDataMapper.Api.Models.LocationServiceInfo;
using HappyTravel.StaticDataMapper.Api.Services.LocationMappingInfo;
using Microsoft.AspNetCore.Mvc;
using Location = HappyTravel.StaticDataMapper.Api.Models.LocationInfo.Location;

namespace HappyTravel.StaticDataMapper.Api.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/{v:apiVersion}/location-mappings")]
    [Produces("application/json")]
    [Permissions(MapperPermissions.Read)]
    public class LocationMappingsController : StaticDataControllerBase
    {
        public LocationMappingsController(ILocationMappingInfoService locationMappingInfoService)
        {
            _locationMappingInfoService = locationMappingInfoService;
        }
        
        
        /// <summary>
        /// Gets location mapping info by given htId
        /// </summary>
        /// <returns></returns>
        [HttpGet("{htId}")]
        [ProducesResponseType(typeof(LocationMapping), (int) HttpStatusCode.OK)]
        public async Task<IActionResult> GetMapping([FromRoute] string htId)
        {
            var (_, isFailure, locationMappingInfo, error) = await _locationMappingInfoService.Get(htId, LanguageCode);

            if (isFailure)
                return BadRequest(error);

            return Ok(locationMappingInfo);
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
        [HttpGet("locations")]
        [ProducesResponseType(typeof(List<Location>), (int) HttpStatusCode.OK)]
        public async Task<IActionResult> GetLocations([FromQuery] AccommodationMapperLocationTypes locationType, [FromQuery] DateTime modified = default, [FromQuery] int skip = 0, [Range(0, 50000)][FromQuery] int top = 50000, CancellationToken cancellationToken = default)
            => Ok(await _locationMappingInfoService.Get(locationType, LanguageCode, modified, skip, top, cancellationToken));
        
        
        private readonly ILocationMappingInfoService _locationMappingInfoService;
    }
}