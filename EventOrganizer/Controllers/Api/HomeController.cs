using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace EventOrganizer.Controllers.Api
{
    [Route("api/sujata/[controller]")]
    [ApiController]
    public class HomeController : ControllerBase
    {
        [HttpGet]
        [Route("getone")]

        public IActionResult getone()
        {
            return Ok();
        }
    }
}
