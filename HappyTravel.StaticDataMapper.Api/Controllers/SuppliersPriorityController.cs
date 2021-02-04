using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using HappyTravel.StaticDataMapper.Data.Models;
using HappyTravel.StaticDataMapper.Data.Models.Accommodations;
using HappyTravel.StaticDataMapper.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HappyTravel.StaticDataMapper.Api.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/{v:apiVersion}/[controller]")]
    [Produces("application/json")]
    [Authorize(Policy = "CanEdit")]
    public class SuppliersPriorityController : StaticDataControllerBase
    {
        public SuppliersPriorityController(ISuppliersPriorityService suppliersPriorityService)
        {
            _suppliersPriorityService = suppliersPriorityService;
        }

        /// <summary>
        /// Sets or updates default priority
        /// </summary>
        /// <param name="suppliersPriority"></param>
        /// <returns></returns>
        [HttpPost]
        [ProducesResponseType((int) HttpStatusCode.NoContent)]
        public async Task<IActionResult> AddOrUpdateDefaultPriority(Dictionary<AccommodationDataTypes, List<Suppliers>> suppliersPriority)
        {
            await _suppliersPriorityService.AddOrUpdate(suppliersPriority);
            return NoContent();
        }

        private readonly ISuppliersPriorityService _suppliersPriorityService;
    }
}