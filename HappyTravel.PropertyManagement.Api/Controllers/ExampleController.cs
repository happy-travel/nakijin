using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace HappyTravel.PropertyManagement.Api.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/{v:apiVersion}/[controller]")]
    [Produces("application/json")]
    public class ExampleController : ControllerBase
    {
        [HttpGet]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        public async Task<IActionResult> Get()
        {
            return await Task.FromResult(Ok());
        }
    }
}
