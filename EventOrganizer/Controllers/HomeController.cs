using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using EventOrganizer.Models;
using EventOrganizer.Repository.Interface;
using Microsoft.AspNetCore.Mvc;

namespace EventOrganizer.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IConfiguration _configuration;
       
        public HomeController(ILogger<HomeController> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        [Obsolete]
        private SqlConnection GetConnection()
        {
            return new SqlConnection(_configuration.GetConnectionString("IEOConnection"));
        }

        public IActionResult Index()
        {
            List<ReviewTestimonial> reviews = GetApprovedReviewsFromDatabase();
            ViewBag.Reviews = reviews;
            return View();
        }

        private List<ReviewTestimonial> GetApprovedReviewsFromDatabase()
        {
            var reviews = new List<ReviewTestimonial>();

            using (SqlConnection con = new SqlConnection(_configuration.GetConnectionString("IEOConnection")))
            {
                SqlCommand cmd = new SqlCommand("SP_GetApprovedReviews", con);
                cmd.CommandType = CommandType.StoredProcedure;
                con.Open();

                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var review = new ReviewTestimonial
                        {
                            UserName = reader.GetString(reader.GetOrdinal("UserName")),
                            ReviewText = reader.GetString(reader.GetOrdinal("ReviewText")),
                            Rating = reader.GetInt32(reader.GetOrdinal("Rating")),
                            CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt"))
                        };
                        reviews.Add(review);
                    }
                }
            }

            return reviews;
        }
        public IActionResult Privacy()
        {
            return View();
        }

        public IActionResult Login()
        {
            return View();
        }
        [HttpPost]
        public IActionResult Login(LoginRequest model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                using (var con = GetConnection())
                {
                    con.Open();
                    SqlCommand cmd = new SqlCommand("SP_Login_CURD", con);
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@TransType", "SELECT-ONE");
                    cmd.Parameters.AddWithValue("@Email", model.Email);
                    cmd.Parameters.AddWithValue("@Password", model.Password);

                    SqlDataReader rdr = cmd.ExecuteReader();

                    if (rdr.Read()) // If user found
                    {
                        string email = rdr["Email"].ToString();
                        string userType = rdr["UserType"].ToString(); // Fetching UserType

                        // Store user info in Session (Optional)
                        HttpContext.Session.SetString("UserEmail", email);
                        HttpContext.Session.SetString("UserType", userType);

                        // Redirect based on UserType
                        if (userType.Equals("Admin", StringComparison.OrdinalIgnoreCase))
                        {
                            return RedirectToAction("Dashboard", "Admin");
                        }
                        else
                        {
                            return RedirectToAction("UserDashboard", "User");
                        }
                    }
                    else
                    {
                        ViewBag.ErrorMessage = "Invalid Email or Password!";
                        return View(model);
                    }
                }
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = ex.Message;
                return View(model);
            }
        }
           public IActionResult Services()
        {
            return View();
        }
        public IActionResult About()
        {
            return View();
        }
        public IActionResult KnowMore()
        {
            return View();
        }
        public IActionResult Venues()
        {
            return View();
        }

        public IActionResult Gallery()
        {
            return View();
        }

       

        public IActionResult BookNow()
        {
            return View();
        }
        public IActionResult Wedding()
        {
            return View();
        }
        public IActionResult Birthday()
        {
            return View();
        }
        public IActionResult Corporate()
        {
            return View();
        }
        public IActionResult Destination()
        {
            return View();
        }
        public IActionResult Catering()
        {
            return View();
        }
        public IActionResult Entertainment()
        {
            return View();
        }
        public IActionResult Photography()
        {
            return View();
        }
        public IActionResult Videography()
        {
            return View();
        }
        public IActionResult Privateparties()
        {
            return View();
        }
        [HttpGet]
        public IActionResult Contact()
        {
            // Retrieve session data for logged-in user
            int? userId = HttpContext.Session.GetInt32("UserId");
            string firstName = HttpContext.Session.GetString("UName");
            string lastName = HttpContext.Session.GetString("ULastName");
            string email = HttpContext.Session.GetString("UserEmail");

            if (userId.HasValue && !string.IsNullOrEmpty(firstName) && !string.IsNullOrEmpty(email))
            {
                // Combine first and last name for display
                ViewBag.UserName = $"{firstName} {lastName}";
                ViewBag.UserEmail = email;
                ViewBag.UserId = userId; // Pass UserId to the view
            }
            else
            {
                ViewBag.UserName = "";
                ViewBag.UserEmail = "";
                ViewBag.UserId = null; // User is not logged in
            }

            return View();
        }


        [HttpPost]
        public IActionResult Contact(ContactModel model)
        {
            if (ModelState.IsValid)
            {
                int? userId = HttpContext.Session.GetInt32("UserId");
                string email = HttpContext.Session.GetString("UserEmail");

                if (userId == null || string.IsNullOrEmpty(email))
                {
                    TempData["ErrorMessage"] = "Session expired. Please log in again.";
                    return RedirectToAction("Index", "Login");
                }

                string connectionString = _configuration.GetConnectionString("IEOConnection");

                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    using (SqlCommand cmd = new SqlCommand("InsertContactMessage", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@UserId", userId.Value);
                        cmd.Parameters.AddWithValue("@Email", email);
                        cmd.Parameters.AddWithValue("@Message", model.Message);

                        con.Open();
                        cmd.ExecuteNonQuery();
                        con.Close();
                    }
                }

                TempData["SuccessMessage"] = "Thank you for contacting us!";
                return RedirectToAction("Contact");
            }
            return View(model);
        }


        public IActionResult Review()
        {
            var reviews = new List<Reviews>();

            using (SqlConnection con = new SqlConnection(_configuration.GetConnectionString("IEOConnection")))
            {
                SqlCommand cmd = new SqlCommand("SELECT ReviewId, UserId, EventId, ReviewText, Rating, CreatedAt, VenueId, ServiceId FROM Reviews WHERE EventId IS NOT NULL", con);
                con.Open();
                SqlDataReader rdr = cmd.ExecuteReader();
                while (rdr.Read())
                {
                    reviews.Add(new Reviews
                    {
                        ReviewId = Convert.ToInt32(rdr["ReviewId"]),
                        UserId = Convert.ToInt32(rdr["UserId"]),
                        EventId = Convert.ToInt32(rdr["EventId"]),
                        ReviewText = rdr["ReviewText"].ToString(),
                        Rating = Convert.ToInt32(rdr["Rating"]),
                        CreatedAt = Convert.ToDateTime(rdr["CreatedAt"]),
                        VenueId = rdr["VenueId"] as int?,
                        ServiceId = rdr["ServiceId"] as int?
                    });
                }
            }

            return View(reviews);  // Pass the reviews to the home page view
        }








        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
