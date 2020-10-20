using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using HappyTravel.PropertyManagement.Api.Services;
using HappyTravel.PropertyManagement.Data.Models;
using HappyTravel.PropertyManagement.Data.Models.Accommodations;
using Microsoft.AspNetCore.Mvc;

namespace HappyTravel.PropertyManagement.Api.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/{v:apiVersion}/[controller]")]
    [Produces("application/json")]
    public class SuppliersPriorityController : Controller
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