using System.Net;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.EdoContracts.Accommodations;
using HappyTravel.StaticDataMapper.Api.Filters.Authorization;
using HappyTravel.StaticDataMapper.Data.Models;
using HappyTravel.StaticDataMapper.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace HappyTravel.StaticDataMapper.Api.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/{v:apiVersion}")]
    [Produces("application/json")]
    [Permissions(MapperPermissions.Read)]
    public class AccommodationsController : StaticDataControllerBase
    {
        public AccommodationsController(IAccommodationService accommodationService)
        {
            _accommodationService = accommodationService;
        }


        /// <summary>
        /// Gets accommodation
        /// </summary>
        /// <param name="supplier">Supplier</param>
        /// <param name="supplierAccommodationCode">Supplier Accommodation code </param>
        /// <returns>Accommodation details</returns>
        [HttpGet("suppliers/{supplier}/accommodations/{supplierAccommodationCode}")]
        [ProducesResponseType(typeof(Accommodation), (int) HttpStatusCode.OK)]
        [ProducesResponseType((int) HttpStatusCode.BadRequest)]
        public async Task<IActionResult> Get(Suppliers supplier, string supplierAccommodationCode)
        {
            var (_, isFailure, result, error) = await _accommodationService.Get(supplier, supplierAccommodationCode, LanguageCode);
            if (isFailure)
                return BadRequest(error);

            return Ok(result);
        }


        /// <summary>
        /// Gets accommodation
        /// </summary>
        /// <param name="accommodationId">Accommodation Id</param>
        /// <returns>Accommodation details</returns>
        [HttpGet("accommodations/{accommodationId}")]
        [ProducesResponseType(typeof(Accommodation), (int) HttpStatusCode.OK)]
        [ProducesResponseType((int) HttpStatusCode.BadRequest)]
        public async Task<IActionResult> Get(int accommodationId)
        {
            var (_, isFailure, result, error) = await _accommodationService.Get(accommodationId, LanguageCode);
            if (isFailure)
                return BadRequest(error);

            return Ok(result);
        }


        private readonly IAccommodationService _accommodationService;
    }
}