using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.EdoContracts.Accommodations;
using HappyTravel.PropertyManagement.Api.Services;
using HappyTravel.PropertyManagement.Data.Models;
using HappyTravel.PropertyManagement.Data.Models.Accommodations;
using Microsoft.AspNetCore.Mvc;

namespace HappyTravel.PropertyManagement.Api.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/{v:apiVersion}")]
    [Produces("application/json")]
    public class AccommodationsController : ControllerBase
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
        [ProducesResponseType((int) HttpStatusCode.OK)]
        [ProducesResponseType(typeof(Accommodation), (int) HttpStatusCode.BadRequest)]
        public async Task<IActionResult> Get(Suppliers supplier, string supplierAccommodationCode)
        {
            var (_, isFailure, result, error) = await _accommodationService.Get(supplier, supplierAccommodationCode);
            if (isFailure)
                return BadRequest(error);

            return Ok(result);
        }


        /// <summary>
        /// Gets accommodation
        /// </summary>
        /// <param name="accommodationId">Accommodation Id</param>
        /// <returns>Accommodation details</returns>
        [HttpGet("suppliers/{supplier}/accommodations/{accommodationId}")]
        [ProducesResponseType((int) HttpStatusCode.OK)]
        [ProducesResponseType(typeof(Accommodation), (int) HttpStatusCode.BadRequest)]
        public async Task<IActionResult> Get(int accommodationId)
        {
            var (_, isFailure, result, error) = await _accommodationService.Get(accommodationId);
            if (isFailure)
                return BadRequest(error);

            return Ok(result);
        }


        /// <summary>
        /// Calculates accommodation final data
        /// </summary>
        /// <param name="id">Accommodation id</param>
        /// <returns></returns>
        [HttpPost("accommodations/{id}/recalculate")]
        [ProducesResponseType((int) HttpStatusCode.NoContent)]
        [ProducesResponseType((int) HttpStatusCode.BadRequest)]
        public async Task<IActionResult> RecalculateData(int id)
        {
            var (_, isFailure, error) = await _accommodationService.RecalculateData(id);
            if (isFailure)
                return BadRequest(error);

            return NoContent();
        }


        /// <summary>
        /// Adds suppliers priority to accommodation
        /// </summary>
        /// <param name="accommodationId"></param>
        /// <param name="suppliersPriorities"></param>
        /// <returns></returns>
        [HttpPost("accommodations/{accommodationId}/add-priorities")]
        [ProducesResponseType((int) HttpStatusCode.NoContent)]
        [ProducesResponseType((int) HttpStatusCode.BadRequest)]
        public async Task<IActionResult> AddSuppliersPriority(int accommodationId,
            [FromBody] Dictionary<AccommodationDataTypes, List<Suppliers>> suppliersPriorities)
        {
            var (_, isFailure, error) =
                await _accommodationService.AddSuppliersPriority(accommodationId, suppliersPriorities);

            if (isFailure)
                return BadRequest(error);

            return NoContent();
        }


        /// <summary>
        ///  Adds manual correction to accommodation
        /// </summary>
        /// <param name="accommodationId"></param>
        /// <param name="accommodation"></param>
        /// <returns></returns>
        [HttpPost("accommodations/{accommodationId}/manual-correction")]
        [ProducesResponseType((int) HttpStatusCode.NoContent)]
        [ProducesResponseType((int) HttpStatusCode.BadRequest)]
        public async Task<IActionResult> AddManualCorrectionToAccommodation(int accommodationId, Accommodation accommodation)
        {
            var (_, isFailure, error) =
                await _accommodationService.AddManualCorrection(accommodationId, accommodation);

            if (isFailure)
                return BadRequest(error);

            return NoContent();
        }


        private readonly IAccommodationService _accommodationService;
    }
}