using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.EdoContracts.Accommodations;
using HappyTravel.Nakijin.Api.Infrastructure;
using HappyTravel.Nakijin.Data.Models;
using HappyTravel.Nakijin.Api.Services;
using HappyTravel.SuppliersCatalog;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HappyTravel.Nakijin.Api.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/{v:apiVersion}")]
    [Produces("application/json")]
    [Authorize]
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
            var (_, isFailure, result, error) =
                await _accommodationService.Get(supplier, supplierAccommodationCode, LanguageCode);
            if (isFailure)
                return BadRequest(error);

            return Ok(result);
        }


        /// <summary>
        /// Gets accommodation
        /// </summary>
        /// <param name="accommodationHtId">Accommodation HtId</param>
        /// <returns>Accommodation details</returns>
        [HttpGet("accommodations/{accommodationHtId}")]
        [ProducesResponseType(typeof(Accommodation), (int) HttpStatusCode.OK)]
        [ProducesResponseType((int) HttpStatusCode.BadRequest)]
        public async Task<IActionResult> Get(string accommodationHtId)
        {
            var (_, isFailure, result, error) = await _accommodationService.Get(accommodationHtId, LanguageCode);
            if (isFailure)
                return BadRequest(error);

            return Ok(result);
        }


        /// <summary>
        ///  Returns a list of accommodation details
        /// </summary>
        /// <param name="skip"></param>
        /// <param name="top"></param>
        /// <param name="suppliers"></param>
        /// <param name="hasDirectContractFilter"></param>
        /// <returns></returns>
        [HttpGet("accommodations")]
        [ProducesResponseType(typeof(List<Accommodation>), (int) HttpStatusCode.OK)]
        public async Task<IActionResult> Get(int skip, int top, [FromQuery] List<string> suppliers,
            [FromQuery] bool? hasDirectContractFilter)
        {
            // For now these filters are enough, if will be more we must add separate model for filters.
            var accommodations = await _accommodationService.Get(skip, top, suppliers.ToSuppliersList(),
                hasDirectContractFilter, LanguageCode);
            return Ok(accommodations);
        }


        /// <summary>
        ///   Gets date of last modified accommodation.
        /// </summary>
        /// <returns>Last changed location modified date</returns>
        [ProducesResponseType(typeof(DateTime), (int) HttpStatusCode.OK)]
        [HttpGet("accommodations/last-modified-date")]
        public async Task<IActionResult> GetLastModifiedDate()
        {
            var lastModifiedDate = await _accommodationService.GetLastModifiedDate();

            return Ok(lastModifiedDate);
        }


        private readonly IAccommodationService _accommodationService;
    }
}