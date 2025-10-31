using EventOrganizer.Models;
using System.Data.SqlClient;
using System.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Reflection;

namespace EventOrganizer.Controllers
{
    public class ReviewController : Controller
    {
        private readonly IConfiguration _configuration;

        public ReviewController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpGet]
        public IActionResult AddReview()
        {
            var model = new ReviewViewModel
            {
                Review = new Reviews(),
                Events = GetEventsDropdown(),
                Venues = GetVenuesDropdown(),
                Services = GetServicesDropdown()
            };

            int? userId = HttpContext.Session.GetInt32("UserId");

            if (userId == null)
            {
                return RedirectToAction("Login", "Home");
            }

            model.Review.UserId = userId.Value;
            model.Review.CreatedAt = DateTime.Now;

            return View(model);
        }

        [HttpPost]
        public IActionResult AddReview(ReviewViewModel model)
        {
            ModelState.Remove("Users");
            ModelState.Remove("Events");
            ModelState.Remove("Venues");
            ModelState.Remove("Services");

            if (ModelState.IsValid)
            {
                using (SqlConnection con = new SqlConnection(_configuration.GetConnectionString("IEOConnection")))
                {
                    SqlCommand cmd = new SqlCommand("SP_AddReview", con);
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@userId", model.Review.UserId);
                    cmd.Parameters.AddWithValue("@eventId", model.Review.EventId);
                    cmd.Parameters.AddWithValue("@venueId", (object?)model.Review.VenueId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@serviceId", (object?)model.Review.ServiceId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@reviewText", model.Review.ReviewText);
                    cmd.Parameters.AddWithValue("@rating", model.Review.Rating);

                    con.Open();
                    cmd.ExecuteNonQuery();
                    con.Close();
                }

                return RedirectToAction("ReviewSuccess");
            }

            // Rebind dropdowns if validation fails
            model.Events = GetEventsDropdown();
            model.Venues = GetVenuesDropdown();
            model.Services = GetServicesDropdown();

            return View(model);
        }



        private List<SelectListItem> GetUsersDropdown()
        {
            var users = new List<SelectListItem>();

            using (SqlConnection con = new SqlConnection(_configuration.GetConnectionString("IEOConnection")))
            {
                SqlCommand cmd = new SqlCommand("SELECT UserId, FirstName + ' ' + LastName AS FullName FROM [User]", con);
                con.Open();
                SqlDataReader rdr = cmd.ExecuteReader();
                while (rdr.Read())
                {
                    users.Add(new SelectListItem
                    {
                        Value = rdr["UserId"].ToString(),
                        Text = rdr["FullName"].ToString()
                    });
                }
            }

            return users;
        }

        private List<SelectListItem> GetEventsDropdown()
        {
            var events = new List<SelectListItem>();

            using (SqlConnection con = new SqlConnection(_configuration.GetConnectionString("IEOConnection")))
            {
                SqlCommand cmd = new SqlCommand("SELECT EventId, EventName FROM Events", con);
                con.Open();
                SqlDataReader rdr = cmd.ExecuteReader();
                while (rdr.Read())
                {
                    events.Add(new SelectListItem
                    {
                        Value = rdr["EventId"].ToString(),
                        Text = rdr["EventName"].ToString()
                    });
                }
            }

            return events;
        }



        private List<SelectListItem> GetVenuesDropdown()
        {
            var venues = new List<SelectListItem>();
            using (SqlConnection con = new SqlConnection(_configuration.GetConnectionString("IEOConnection")))
            {
                SqlCommand cmd = new SqlCommand("SELECT VenueId, VenueName FROM Venue", con);
                con.Open();
                SqlDataReader rdr = cmd.ExecuteReader();
                while (rdr.Read())
                {
                    venues.Add(new SelectListItem
                    {
                        Value = rdr["VenueId"].ToString(),
                        Text = rdr["VenueName"].ToString()
                    });
                }
            }
            return venues;
        }

        private List<SelectListItem> GetServicesDropdown()
        {
            var services = new List<SelectListItem>();
            using (SqlConnection con = new SqlConnection(_configuration.GetConnectionString("IEOConnection")))
            {
                SqlCommand cmd = new SqlCommand("SELECT ServiceId, ServiceName FROM Services", con);
                con.Open();
                SqlDataReader rdr = cmd.ExecuteReader();
                while (rdr.Read())
                {
                    services.Add(new SelectListItem
                    {
                        Value = rdr["ServiceId"].ToString(),
                        Text = rdr["ServiceName"].ToString()
                    });
                }
            }
            return services;
        }


        public IActionResult ReviewSuccess()
        {
            return View(); // Create a simple confirmation page
        }




        public IActionResult Testimonial()
        {
            // Fetch the approved reviews using the stored procedure
            List<ReviewTestimonial> reviews = GetApprovedReviewsFromDatabase();

            // Pass the reviews to the View using ViewBag
            ViewBag.Reviews = reviews;

            return View();
        }

        // Method to Fetch Approved Reviews from Database using SP_GetApprovedReviews
        private List<ReviewTestimonial> GetApprovedReviewsFromDatabase()
        {
            var reviews = new List<ReviewTestimonial>();

            // Open a SQL connection using the connection string
            using (SqlConnection con = new SqlConnection(_configuration.GetConnectionString("IEOConnection")))
            {
                SqlCommand cmd = new SqlCommand("SP_GetApprovedReviews", con);
                cmd.CommandType = CommandType.StoredProcedure; // Specify it's a stored procedure
                con.Open(); // Open the connection

                // Execute the command and read the results
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        // Create a new ReviewTestimonial object for each row
                        var review = new ReviewTestimonial
                        {
                            UserName = reader.GetString(reader.GetOrdinal("UserName")),
                            ReviewText = reader.GetString(reader.GetOrdinal("ReviewText")),
                            Rating = reader.GetInt32(reader.GetOrdinal("Rating")),
                            CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt"))
                        };
                        reviews.Add(review); // Add the review to the list
                    }
                }
            }

            return reviews; // Return the list of reviews
        }

    }
}
        
