using Microsoft.AspNetCore.Mvc;
using EventOrganizer.Models;

namespace EventOrganizer.Controllers
{
    public class BaseController : Controller
    {
        protected LoginResponse GetUserSession()
        {
            LoginResponse? Sessionobjs = new LoginResponse();
            // Retrieve JSON string from session
            string? jsonString = HttpContext.Session.GetString("LoginSession");

            if (!string.IsNullOrEmpty(jsonString))
            {
                // Deserialize to object
                Sessionobjs = System.Text.Json.JsonSerializer.Deserialize<LoginResponse>(jsonString);
            }
            return Sessionobjs;
        }




    }




}

