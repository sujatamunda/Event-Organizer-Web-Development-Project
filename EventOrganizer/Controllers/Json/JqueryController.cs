using Microsoft.AspNetCore.Mvc;

namespace EventOrganizer.Controllers.Json
{
    public class JqueryController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
