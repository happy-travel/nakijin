using System.Net;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.StaticDataMapper.Api.Filters.Authorization;
using HappyTravel.StaticDataMapper.Api.Models.LocationInfo;
using HappyTravel.StaticDataMapper.Api.Services.LocationMappingInfo;
using Microsoft.AspNetCore.Mvc;

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
            var (_, isFailure, locationMappingInfo, error) = await _locationMappingInfoService.Get(htId);

            if (isFailure)
                return BadRequest(error);

            return Ok(locationMappingInfo);
        }
        
        private readonly ILocationMappingInfoService _locationMappingInfoService;
    }
}