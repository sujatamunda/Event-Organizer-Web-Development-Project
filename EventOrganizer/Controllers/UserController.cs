using EventOrganizer.Models;
using System.Data.SqlClient;
using Microsoft.AspNetCore.Mvc;
using EventOrganizer.Repository.Interface;
using System.Data;
using Microsoft.Win32;
using System.Reflection;
using System;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Logging;
using System.Text;

namespace EventOrganizer.Controllers
{
    public class UserController : Controller
    {

        public readonly ILogger<UserController> _logger;
        private readonly IConfiguration _configuration;

        public UserController(ILogger<UserController> logger, IConfiguration configuration)
        {

            _logger = logger;
            _configuration = configuration;
        }


        [Obsolete]
        private SqlConnection GetConnection()
        {
            return new SqlConnection(_configuration.GetConnectionString("IEOConnection"));
        }


        //public IActionResult Index()
        //{
        //    return View();
        //}


        public IActionResult Dashboard()
        {
            //var userSession = GetUserSession();
            //if (userSession.UserId == 0 || userSession.UserId == null)
            //{
            //    return RedirectToAction("Index", "Login");
            //}
            //return View(userSession);

            //string? uid = HttpContext.Session.GetString("UserId");
            //if (uid == null)
            //{
            //    return RedirectToAction("Index", "Login");
            //}


            int? userId = HttpContext.Session.GetInt32("UserId"); // 🔥 Declare userId properly

            if (userId == null || userId == 0)
            {
                return RedirectToAction("Index", "Login");
            }
            Console.WriteLine("Retrieved UserId from session: " + userId); // Debugging


            string? firstName = HttpContext.Session.GetString("UName");
            string? lastName = HttpContext.Session.GetString("ULastName");

            ViewBag.FullName = $"{firstName} {lastName}";


            // Fetch user bookings
            List<BookingViewModel> bookings = GetUserBookings(userId.Value);

            ViewBag.TotalBookings = bookings.Count;
            ViewBag.PendingPayments = bookings.Count(b => b.PaymentStatus == "Unpaid");
            ViewBag.ApprovedBookings = bookings.Count(b => b.BookingStatus == "Approved");
            ViewBag.PendingBookings = bookings.Count(b => b.BookingStatus == "Pending");

            return View(bookings);

        }



        private List<BookingViewModel> GetUserBookings(int userId)
        {
            List<BookingViewModel> bookings = new List<BookingViewModel>();

            using (SqlConnection con = new SqlConnection(_configuration.GetConnectionString("IEOConnection")))
            {
                SqlCommand cmd = new SqlCommand("GetUserBookings", con);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@UserId", userId);

                con.Open();
                SqlDataReader rdr = cmd.ExecuteReader();
                while (rdr.Read())
                {
                    bookings.Add(new BookingViewModel
                    {
                        UserId = Convert.ToInt32(rdr["UserId"]),
                        BookingId = Convert.ToInt32(rdr["BookingId"]),
                        EventName = rdr["EventName"].ToString(),
                        VenueName = rdr["VenueName"].ToString(),
                        Location = rdr["Location"].ToString(),

                        // Split services only if it's not null or empty
                        SelectedServiceName = string.IsNullOrEmpty(rdr["Services"].ToString())
                            ? new List<string>()  // Return an empty list if no services
                            : rdr["Services"].ToString().Split(',').Select(s => s.Trim()).ToList(),

                        Capacity = Convert.ToInt32(rdr["Capacity"]),
                        EventDate = Convert.ToDateTime(rdr["EventDate"]),
                        BookingStatus = rdr["BookingStatus"].ToString(),

                        // Mapping PaymentStatus here
                        PaymentStatus = rdr["PaymentStatus"] == DBNull.Value
                            ? "Unpaid"
                            : rdr["PaymentStatus"].ToString()
                    });
                }
                rdr.Close();
                con.Close();
            }

            return bookings;
        }




        public IActionResult MyBookings()
        {
            // Retrieve UserId from session
            int? userId = HttpContext.Session.GetInt32("UserId");

            // If no valid UserId, redirect to login page
            if (userId == null || userId == 0)
            {
                return RedirectToAction("Index", "Login");
            }

            // Fetch user bookings using the same logic as Dashboard
            List<BookingViewModel> bookings = GetUserBookings(userId.Value);

            // Pass bookings to the view
            return View(bookings);
        }






        [HttpGet]
        public IActionResult EditBooking(int id)
        {
            BookingViewModel booking = new BookingViewModel();

            using (var con = GetConnection())
            {
                con.Open();

                // Get Booking details
                SqlCommand cmd = new SqlCommand("SP_GetBookingById", con)
                {
                    CommandType = CommandType.StoredProcedure
                };
                cmd.Parameters.AddWithValue("@BookingId", id);

                using (SqlDataReader rdr = cmd.ExecuteReader())
                {
                    if (rdr.Read())
                    {
                        booking.BookingId = (int)rdr["BookingId"];
                        booking.EventName = rdr["EventName"].ToString();

                        booking.VenueName = rdr["Venues"].ToString().Trim(); // This will hold the CURRENT venue
                        booking.SelectedLocation = rdr["Location"].ToString();
                        booking.EventDate = Convert.ToDateTime(rdr["EventDate"]);
                        booking.Capacity = Convert.ToInt32(rdr["Capacity"]);
                        booking.SelectedServiceName = rdr["Services"].ToString().Split(',').ToList();
                    }
                }

                // Get all venues EXCEPT the one already selected
                booking.Venues = new List<SelectListItem>();
                SqlCommand venueCmd = new SqlCommand("SELECT VenueName FROM Venue WHERE VenueName != @CurrentVenue", con);
                venueCmd.Parameters.AddWithValue("@CurrentVenue", booking.VenueName);

                using (SqlDataReader rdr = venueCmd.ExecuteReader())
                {
                    while (rdr.Read())
                    {
                        string venue = rdr["VenueName"].ToString();
                        booking.Venues.Add(new SelectListItem
                        {
                            Value = venue,
                            Text = venue
                        });
                    }
                }

                // Load Services
                booking.Services = new List<SelectListItem>();
                SqlCommand serviceCmd = new SqlCommand("SELECT ServiceName FROM Services", con);
                using (SqlDataReader rdr = serviceCmd.ExecuteReader())
                {
                    while (rdr.Read())
                    {
                        string service = rdr["ServiceName"].ToString();
                        booking.Services.Add(new SelectListItem
                        {
                            Value = service,
                            Text = service,
                            Selected = booking.SelectedServiceName.Contains(service)
                        });
                    }
                }

                // Load Locations
                booking.Locations = new List<SelectListItem>();
                SqlCommand locCmd = new SqlCommand("SELECT DISTINCT Location FROM Venue", con);
                using (SqlDataReader rdr = locCmd.ExecuteReader())
                {
                    while (rdr.Read())
                    {
                        string city = rdr["Location"].ToString();
                        booking.Locations.Add(new SelectListItem
                        {
                            Value = city,
                            Text = city,
                            Selected = booking.SelectedLocation == city
                        });
                    }
                }
            }

            return View(booking);
        }




       



        [HttpPost]
        public IActionResult EditBooking(BookingViewModel booking)
        {
            if (ModelState.IsValid)
            {
                using (var con = GetConnection())
                {
                    con.Open();

                    SqlCommand cmd = new SqlCommand("SP_UpdateBooking", con)
                    {
                        CommandType = CommandType.StoredProcedure
                    };

                    cmd.Parameters.AddWithValue("@BookingId", booking.BookingId);
                    cmd.Parameters.AddWithValue("@VenueId", booking.VenueId);
                    cmd.Parameters.AddWithValue("@Location", booking.SelectedLocation);
                    cmd.Parameters.AddWithValue("@EventDate", booking.EventDate);
                    cmd.Parameters.AddWithValue("@Capacity", booking.Capacity);

                    string selectedServices = string.Join(",", booking.SelectedServiceName);
                    cmd.Parameters.AddWithValue("@Services", selectedServices);

                    cmd.ExecuteNonQuery();
                }

                return RedirectToAction("Dashboard");
            }

            // Re-populate values if ModelState is invalid
            booking.Services = new List<SelectListItem>();
            booking.Locations = new List<SelectListItem>();
            booking.Venues = new List<SelectListItem>();

            using (var con = GetConnection())
            {
                con.Open();

                // 🟡 FIX: Re-fetch EventName from DB using BookingId
                SqlCommand eventCmd = new SqlCommand("SP_GetBookingById", con)
                {
                    CommandType = CommandType.StoredProcedure
                };
                eventCmd.Parameters.AddWithValue("@BookingId", booking.BookingId);
                using (SqlDataReader rdr = eventCmd.ExecuteReader())
                {
                    if (rdr.Read())
                    {
                        booking.EventName = rdr["EventName"].ToString();
                    }
                }

                // ✅ Services
                SqlCommand serviceCmd = new SqlCommand("SELECT ServiceName FROM Services", con);
                using (SqlDataReader rdr = serviceCmd.ExecuteReader())
                {
                    while (rdr.Read())
                    {
                        string service = rdr["ServiceName"].ToString();
                        booking.Services.Add(new SelectListItem
                        {
                            Value = service,
                            Text = service,
                            Selected = booking.SelectedServiceName.Contains(service)
                        });
                    }
                }

                // ✅ Locations
                SqlCommand locCmd = new SqlCommand("SELECT DISTINCT Location FROM Venue", con);
                using (SqlDataReader rdr = locCmd.ExecuteReader())
                {
                    while (rdr.Read())
                    {
                        string city = rdr["Location"].ToString();
                        booking.Locations.Add(new SelectListItem
                        {
                            Value = city,
                            Text = city,
                            Selected = booking.SelectedLocation == city
                        });
                    }
                }

                // ✅ Venues filtered by selected location
                SqlCommand venueCmd = new SqlCommand("SELECT VenueName FROM Venue WHERE Location = @Location", con);
                venueCmd.Parameters.AddWithValue("@Location", booking.SelectedLocation ?? "");
                using (SqlDataReader rdr = venueCmd.ExecuteReader())
                {
                    while (rdr.Read())
                    {
                        string venue = rdr["VenueName"].ToString();
                        booking.Venues.Add(new SelectListItem
                        {
                            Value = venue,
                            Text = venue,
                            Selected = booking.VenueName == venue
                        });
                    }
                }
            }

            return View(booking);
        }





        [HttpGet]
        public IActionResult CancelBooking(int id)
        {
            using (var con = GetConnection())
            {
                con.Open();
                SqlCommand cmd = new SqlCommand("SP_CancelBooking", con)
                {
                    CommandType = CommandType.StoredProcedure
                };
                cmd.Parameters.AddWithValue("@BookingId", id);
                cmd.ExecuteNonQuery();
            }

            TempData["Message"] = "Booking has been cancelled.";
            return RedirectToAction("Dashboard");
        }

















        public IActionResult Events()
        {
            return View();
        }
      


        [HttpGet]
        public IActionResult Bookings()
        {
            var model = new Bookings();

            // ✅ Retrieve UserId from session
            int? userId = HttpContext.Session.GetInt32("UserId");

            if (userId.HasValue)
            {
                model.UserId = userId.Value;  // ✅ Auto-fill UserId
            }
            else
            {
                TempData["BookingError"] = "Invalid User Session. Please log in to book an event.";
                return RedirectToAction("Index", "Login"); // Redirect to login page
            }

            return View(model);
        }

        [HttpPost]
        public IActionResult Bookings(Bookings booking)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    string connStr = _configuration.GetConnectionString("IEOConnection");
                    using (SqlConnection con = new SqlConnection(connStr))
                    {
                        con.Open();
                        using (SqlCommand cmd = new SqlCommand("InsertBooking", con))
                        {
                            cmd.CommandType = CommandType.StoredProcedure;

                            cmd.Parameters.AddWithValue("@UserId", booking.UserId);
                            cmd.Parameters.AddWithValue("@EventName", booking.EventName);
                            cmd.Parameters.AddWithValue("@Location", booking.Location);
                            cmd.Parameters.AddWithValue("@Venues", booking.VenueName);
                            cmd.Parameters.AddWithValue("@Services", string.IsNullOrEmpty(booking.Services) ? (object)DBNull.Value : booking.Services);
                            cmd.Parameters.AddWithValue("@Capacity", booking.Capacity);
                            cmd.Parameters.AddWithValue("@Description", string.IsNullOrEmpty(booking.Description) ? (object)DBNull.Value : booking.Description);
                            cmd.Parameters.AddWithValue("@EventDate", booking.EventDate == DateTime.MinValue ? (object)DBNull.Value : booking.EventDate);
                            cmd.Parameters.AddWithValue("@BookinStatus", "Pending");

                            int rowsAffected = cmd.ExecuteNonQuery();
                            if (rowsAffected > 0)
                            {
                                ViewBag.SuccessMsg = "Booking successful!";
                                ModelState.Clear();
                                return View(new Bookings()); // Return a fresh form
                            }
                            else
                            {
                                ViewBag.ErrorMsg = "Failed to book. Try again!";
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    ViewBag.ErrorMsg = "Error: " + ex.Message;
                    Console.WriteLine(ex.ToString()); // Debugging info
                }
            }
            return View(booking);
        }






        public IActionResult Reviews()
        {
            return View();
        }


        [HttpPost]
        public IActionResult Reviews(Reviews review)
        {
            string connectionString = _configuration.GetConnectionString("IEOConnection");

            using (SqlConnection con = new SqlConnection(connectionString))
            {
                SqlCommand cmd = new SqlCommand("SP_Review", con);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@UserId", review.UserId);
                cmd.Parameters.AddWithValue("@EventId", review.EventId);
                cmd.Parameters.AddWithValue("@ReviewText", review.ReviewText);
                cmd.Parameters.AddWithValue("@CreatedAt", review.CreatedAt);

                con.Open();
                int result = cmd.ExecuteNonQuery();
                con.Close();

                if (result > 0)
                {
                    ViewBag.SuccessMsg = "Review submitted successfully!";
                }
                else
                {
                    ViewBag.ErrorMsg = "Failed to submit review.";
                }
            }
            return View(review);
        }

        public IActionResult Customizations()
        {
            return View();
        }


       




        [HttpGet]
        public IActionResult CreateBooking()
        {
            var model = new BookingViewModel
            {
                Events = GetEvents(),
                Locations = GetLocations(),
                Services = GetServices()
                // Venues excluded initially, as they are loaded dynamically via city selection
            };

            return View("CreateBooking", model);
        }



        


        [HttpPost]
        public IActionResult CreateBooking(BookingViewModel model)
        {
            // Re-populate dropdowns in case of error
            model.Events = GetEvents();
            model.Services = GetServices();
            model.Venues = GetVenuesByCityList(model.SelectedLocation,model.EventDate);
            model.Locations = GetLocations();
            

            // Get UserId from session
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return RedirectToAction("Index", "Login");

            model.UserId = userId.Value;

            // Event Name: use custom or selected
            string eventName = !string.IsNullOrEmpty(model.CustomEvent)
                ? model.CustomEvent
                : model.Events.FirstOrDefault(e => e.Value == model.EventId.ToString())?.Text ?? "";

            // Venue Name: custom or selected
            string venueName = "";

            if ((model.VenueId == 0 || model.VenueId.ToString() == "Other") && !string.IsNullOrEmpty(model.CustomVenue))
            {
                venueName = model.CustomVenue;

                // Optional: Insert custom venue into your Venue table (if needed)
                using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("IEOConnection")))
                {
                    SqlCommand cmd = new SqlCommand("InsertCustomVenue", conn);
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@VenueName", venueName);

                    conn.Open();
                    cmd.ExecuteNonQuery(); // You can ignore return if you don't need VenueId
                    conn.Close();
                }
            }
            else if (model.VenueId > 0)
            {
                // Fetch VenueName from DB based on VenueId
                using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("IEOConnection")))
                {
                    SqlCommand cmd = new SqlCommand("SELECT VenueName FROM Venue WHERE VenueId = @VenueId", conn);
                    cmd.Parameters.AddWithValue("@VenueId", model.VenueId);
                    conn.Open();
                    var result = cmd.ExecuteScalar();
                    venueName = result?.ToString() ?? "";
                    conn.Close();
                }

                if (string.IsNullOrEmpty(venueName))
                {
                    ModelState.AddModelError("", "Selected venue is invalid.");
                    return View("CreateBooking", model);
                }
            }
            else
            {
                ModelState.AddModelError("", "Please select or enter a venue.");
                return View("CreateBooking", model);
            }


            // Service Name(s)
            string serviceName = string.IsNullOrEmpty(model.CustomService)
                ? string.Join(", ", model.SelectedServiceName ?? new List<string>())
                : model.CustomService;

            try
            {
                using (SqlConnection con = new SqlConnection(_configuration.GetConnectionString("IEOConnection")))
                {
                    con.Open();

                    // Check if the venue is already booked on that date
                    using (SqlCommand checkCmd = new SqlCommand("SP_CheckVenueAvailability", con))
                    {
                        checkCmd.CommandType = CommandType.StoredProcedure;
                        checkCmd.Parameters.AddWithValue("@VenueId", model.VenueId);
                        checkCmd.Parameters.AddWithValue("@EventDate", model.EventDate);

                        object result = checkCmd.ExecuteScalar();
                        int isAvailable = (result != null && result != DBNull.Value) ? Convert.ToInt32(result) : 0;


                        if (isAvailable > 0)
                        {
                            TempData["VenueBooked"] = "This venue is already booked on the selected date.";
                            return View("CreateBooking", model);
                        }
                    }

                    // Insert the booking
                    using (SqlCommand cmd = new SqlCommand("InsertBooking", con))
                    {

                        string bookingNo = getBookingNo(model.EventDate.Year);

                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@UserId", model.UserId);
                        cmd.Parameters.AddWithValue("@BookingNo", bookingNo);
                        cmd.Parameters.AddWithValue("@EventName", eventName);
                        cmd.Parameters.AddWithValue("@Location", model.SelectedLocation);
                        cmd.Parameters.AddWithValue("@VenueId", model.VenueId); // Pass VenueId, not VenueName

                        cmd.Parameters.AddWithValue("@EventDate", model.EventDate);
                        cmd.Parameters.AddWithValue("@Services", serviceName);
                        cmd.Parameters.AddWithValue("@Capacity", model.Capacity);
                        cmd.Parameters.AddWithValue("@Description", model.Description ?? "");
                        //cmd.Parameters.AddWithValue("@PaymentStatus", "Pending");

                        int rowsAffected = cmd.ExecuteNonQuery();

                        //if (rowsAffected > 0)
                        //{
                        //    //TempData["BookingSuccess"] = "Booking created successfully!";
                        //    //return RedirectToAction("Dashboard", "User");
                        //}
                        //else
                        //{
                        //    ModelState.AddModelError("", "Something went wrong while saving your booking.");
                        //    return View("CreateBooking", model);
                        //}


                        if (rowsAffected > 0)
                        {
                            ViewBag.SuccessMsg = "Booking inserted successfully!";
                            ModelState.Clear(); // Clear the form for new input


                            TempData["SuccessMsg"] = "Booking inserted successfully!";
                            return RedirectToAction("MyBookings", "User");
                        }
                        else
                        {
                            ViewBag.ErrorMsg = "Booking creation failed. Please try again.";
                            return View("CreateBooking", model);

                            //    return View("CreateBooking", new BookingViewModel
                            //    {
                            //        Events = GetEvents(),
                            //        Locations = GetLocations(),
                            //        Services = GetServices(),
                            //        Venues = GetVenuesByCityList(model.SelectedLocation,model.EventDate)
                            //    });
                            //}
                            //else
                            //{
                            //    // Refill dropdowns if there's an error
                            //    ViewBag.ErrorMsg = "Booking creation failed. Please try again.";
                            //    model.Events = GetEvents();
                            //    model.Locations = GetLocations();
                            //    model.Services = GetServices();
                            //    model.Venues = GetVenuesByCityList(model.SelectedLocation,model.EventDate);

                            //    return View("CreateBooking", model);
                        }

                    }
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "An error occurred: " + ex.Message);
                return View("CreateBooking", model);
            }
        }


        private string getBookingNo(int year)
        {
            int maxbookingNo = 0;
            string bookingNo = "EMS" + year;
            using (SqlConnection con = new SqlConnection(_configuration.GetConnectionString("IEOConnection")))
            {
                using (SqlCommand cmd = new SqlCommand("SP_GetBookingNo", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@year", year);


                    con.Open();
                    SqlDataReader rdr = cmd.ExecuteReader();
                    while (rdr.Read())
                    {
                        maxbookingNo = Convert.ToInt32(rdr["bookingNo"].ToString());
                        if (maxbookingNo.ToString().Length == 1)
                            bookingNo += "000" + maxbookingNo;
                        else if (maxbookingNo.ToString().Length == 2)
                            bookingNo += "00" + maxbookingNo;
                        else if (maxbookingNo.ToString().Length == 3)
                            bookingNo += "0" + maxbookingNo;
                        else
                            bookingNo += maxbookingNo;
                    }
                    con.Close();
                }
            }
            return bookingNo;
        }

        private List<SelectListItem> GetEvents()
        {
            List<SelectListItem> events = new();
            using (SqlConnection con = new SqlConnection(_configuration.GetConnectionString("IEOConnection")))
            {
                using (SqlCommand cmd = new SqlCommand("SP_GetEvents", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    con.Open();
                    SqlDataReader rdr = cmd.ExecuteReader();
                    while (rdr.Read())
                    {
                        events.Add(new SelectListItem
                        {
                            Value = rdr["Value"].ToString(),
                            Text = rdr["Text"].ToString()
                        });
                    }
                    con.Close();
                }
            }

            

            return events;
        }

        private List<SelectListItem> GetServices()
        {
            List<SelectListItem> services = new();
            using (SqlConnection con = new SqlConnection(_configuration.GetConnectionString("IEOConnection")))
            {
                using (SqlCommand cmd = new SqlCommand("SP_GetServices", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    con.Open();
                    SqlDataReader rdr = cmd.ExecuteReader();
                    while (rdr.Read())
                    {
                        services.Add(new SelectListItem
                        {
                            Value = rdr["Value"].ToString(),
                            Text = rdr["Text"].ToString()
                        });
                    }
                    con.Close();
                }
            }
            return services;
        }

        private List<SelectListItem> GetLocations()
        {
            List<SelectListItem> locationsList = new List<SelectListItem>();
            using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("IEOConnection")))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand("SP_GetLocations", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    SqlDataReader reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        locationsList.Add(new SelectListItem
                        {
                            Value = reader["Value"].ToString(),
                            Text = reader["Text"].ToString()
                        });
                    }
                    reader.Close();
                }
            }
            return locationsList;
        }

        [HttpGet]
        public JsonResult GetVenuesByCity(string city, string eventDate)
        {
            //eventDate = Convert.ToDateTime("30-Apr-2025");
            List<SelectListItem> venues = new();

            using (SqlConnection con = new SqlConnection(_configuration.GetConnectionString("IEOConnection")))
            {
                using (SqlCommand cmd = new SqlCommand("SP_GetVenuesByCity", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@City", city);
                    cmd.Parameters.AddWithValue("@EventDate", eventDate);

                    con.Open();
                    SqlDataReader rdr = cmd.ExecuteReader();
                    while (rdr.Read())
                    {
                        venues.Add(new SelectListItem
                        {
                            Value = rdr["Value"].ToString(),
                            Text = rdr["Text"].ToString()
                        });
                    }
                }
            }

            return Json(venues);
        }


        private List<SelectListItem> GetVenuesByCityList(string city, DateTime EventDate)
        {
            List<SelectListItem> venues = new();

            using (SqlConnection con = new SqlConnection(_configuration.GetConnectionString("IEOConnection")))
            {
                using (SqlCommand cmd = new SqlCommand("SP_GetVenuesByCity", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@City", city);
                    cmd.Parameters.AddWithValue("@EventDate", EventDate);
                    con.Open();
                    SqlDataReader rdr = cmd.ExecuteReader();
                    while (rdr.Read())
                    {
                        venues.Add(new SelectListItem
                        {
                            Value = rdr["Value"].ToString(),
                            Text = rdr["Text"].ToString()
                        });
                    }
                }
            }

            return venues;
        }



        private List<string> GetServiceNamesByIds(List<int> serviceIds)
        {
            List<string> serviceNames = new();

            if (serviceIds == null || serviceIds.Count == 0)
                return serviceNames;

            using (SqlConnection con = new SqlConnection(_configuration.GetConnectionString("IEOConnection")))
            {
                con.Open();
                foreach (var id in serviceIds)
                {
                    using (SqlCommand cmd = new SqlCommand("SELECT ServiceName FROM Services WHERE ServiceId = @Id", con))
                    {
                        cmd.Parameters.AddWithValue("@Id", id);
                        var result = cmd.ExecuteScalar();
                        if (result != null)
                            serviceNames.Add(result.ToString());
                    }
                }
                con.Close();
            }

            return serviceNames;
        }

       
        [HttpGet]
        public JsonResult GetAvailableDates(int venueId)
        {
            List<string> availableDates = new List<string>();

            using (SqlConnection con = new SqlConnection(_configuration.GetConnectionString("IEOConnection")))
            {
                using (SqlCommand cmd = new SqlCommand("SP_GetAvailableDatesForVenue", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@VenueId", venueId);
                    cmd.Parameters.AddWithValue("@StartDate", DateTime.Now.Date);
                    cmd.Parameters.AddWithValue("@DaysAhead", 10); // You can modify this as needed

                    con.Open();
                    SqlDataReader rdr = cmd.ExecuteReader();
                    while (rdr.Read())
                    {
                        availableDates.Add(Convert.ToDateTime(rdr["AvailableDate"]).ToString("yyyy-MM-dd")); // Format as needed
                    }
                }
            }

            return Json(availableDates);
        }


        public bool IsVenueAvailable(int venueId, DateTime eventDate)
        {
            bool isAvailable = false;

            using (SqlConnection con = new SqlConnection(_configuration.GetConnectionString("IEOConnection")))
            {
                SqlCommand cmd = new SqlCommand("SP_CheckVenueAvailability", con);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@VenueId", venueId);
                cmd.Parameters.AddWithValue("@EventDate", eventDate);

                con.Open();
                var result = cmd.ExecuteScalar();
                if (result != null && Convert.ToInt32(result) == 1)
                {
                    isAvailable = true; // Venue is available
                }
                con.Close();
            }

            return isAvailable;
        }



        private BookingDetails GetBookingDetails(int bookingId)
        {
            // Fetch the connection string from appsettings.json
            string connectionString = _configuration.GetConnectionString("IEOConnection");

            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();

                var command = new SqlCommand("SP_GetBookingDetailsById", connection)
                {
                    CommandType = CommandType.StoredProcedure
                };
                command.Parameters.AddWithValue("@BookingId", bookingId);

                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        // Map your result to a BookingDetails object
                        return new BookingDetails
                        {
                            Venue = reader["Venue"].ToString(),
                            Services = reader["Services"].ToString(),
                        };
                    }
                }
            }

            return null; // If no details found, return null
        }

        // This method calculates the full payment based on venue and services
        private decimal CalculateFullPayment(string venue, string services)
        {
            decimal venueCost = 0m;
            decimal serviceCost = 0m;

            // Fetch venue cost based on the selected venue (for example)
            venueCost = GetVenueCost(venue); // Call a method to fetch venue cost

            // Fetch service costs (assuming services are stored as a comma-separated string)
            string[] selectedServices = services.Split(',');

            foreach (var service in selectedServices)
            {
                serviceCost += GetServiceCost(service); // Call a method to get the cost for each service
            }

            // Total full payment is the sum of venue and service costs
            return venueCost + serviceCost;
        }

        // Method to get the venue cost
        private decimal GetVenueCost(string venue)
        {
            // Fetch the connection string from appsettings.json
            string connectionString = _configuration.GetConnectionString("IEOConnection");

            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                var command = new SqlCommand("SP_GetVenueCost", connection)
                {
                    CommandType = CommandType.StoredProcedure
                };
                command.Parameters.AddWithValue("@Venue", venue);

                var cost = (decimal)command.ExecuteScalar(); // Fetch the single cost value
                return cost;
            }
        }

        


        private decimal GetServiceCost(string service)
        {
            string connectionString = _configuration.GetConnectionString("IEOConnection");

            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                var command = new SqlCommand("SP_GetServiceCost", connection)
                {
                    CommandType = CommandType.StoredProcedure
                };
                command.Parameters.AddWithValue("@Service", service);

                var result = command.ExecuteScalar();

                if (result == null || result == DBNull.Value)
                {
                    return 0; // Or throw a custom exception, or handle as needed
                }

                return Convert.ToDecimal(result);
            }
        }



        


        [HttpGet]
        public IActionResult Payment(int bookingId)
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // Fetch the booking details, including venue and services
            var bookingDetails = GetBookingDetails(bookingId);

            if (bookingDetails == null)
            {
                // Handle the case where booking details could not be found
                TempData["Error"] = "Booking not found.";
                return RedirectToAction("Error", "Home");
            }

            var venue = bookingDetails.Venue;
            var services = bookingDetails.Services;

            // Ensure that venue and services are valid before proceeding with payment calculation
            if (string.IsNullOrEmpty(venue) || string.IsNullOrEmpty(services))
            {
                TempData["Error"] = "Invalid venue or services associated with the booking.";
                return RedirectToAction("Error", "Home");
            }

            // Calculate the full payment (for example, based on venue capacity and selected services)
            decimal fullPaymentAmount = CalculateFullPayment(venue, services);

            // If full payment is 0 (invalid data), return an error
            if (fullPaymentAmount <= 0)
            {
                TempData["Error"] = "Unable to calculate payment due to missing or incorrect data.";
                return RedirectToAction("Error", "Home");
            }

            // Calculate the advanced payment (e.g., 20% of the full payment)
            decimal advancedPaymentAmount = fullPaymentAmount * 0.20m;

            var paymentViewModel = new PaymentViewModel
            {
                BookingId = bookingId,
                FullPaymentAmount = fullPaymentAmount,
                Amount = advancedPaymentAmount,  // Show the advanced payment amount by default
            };

            return View(paymentViewModel);
        }


        [HttpPost]
        public IActionResult Payment(PaymentViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Validate card details
                if (model.CardNumber.Length != 16 || model.CVV.Length != 3)
                {
                    ModelState.AddModelError("", "Invalid card details.");
                    return View(model);
                }

                // Get UserId from Session
                int? userId = HttpContext.Session.GetInt32("UserId");
                if (userId == null)
                {
                    return RedirectToAction("Login", "Account");
                }

                // Generate TransactionId (unique identifier for the payment transaction)
                string transactionId = Guid.NewGuid().ToString();

                using (SqlConnection con = new SqlConnection(_configuration.GetConnectionString("IEOConnection")))
                {
                    SqlCommand cmd = new SqlCommand("SP_MakePayment", con);
                    cmd.CommandType = CommandType.StoredProcedure;

                    // Add parameters to the stored procedure
                    cmd.Parameters.AddWithValue("@BookingId", model.BookingId);
                    cmd.Parameters.AddWithValue("@UserId", userId);
                    cmd.Parameters.AddWithValue("@Amount", model.Amount);
                    cmd.Parameters.AddWithValue("@PaymentMethod", model.PaymentMethod);
                    cmd.Parameters.AddWithValue("@PaymentType", model.PaymentType); // Advance or Full
                    cmd.Parameters.AddWithValue("@CardNumber", model.CardNumber);   // Card Number
                    cmd.Parameters.AddWithValue("@PaymentDate", DateTime.Now);      // Payment Date
                    cmd.Parameters.AddWithValue("@TransactionId", transactionId);   // TransactionId

                    con.Open();
                    cmd.ExecuteNonQuery();
                }

                // After successful payment, set PaymentStatus
                model.PaymentStatus = model.PaymentType == "Full" ? "Paid" : "Partial"; // Based on payment type

                // Set the TransactionId after the payment is processed
                model.TransactionId = transactionId;

                // After successful payment, update payment status and transaction details
                TempData["Success"] = "Payment successful!";
                return View("PaymentSuccess", model); // Pass the model with the status and transactionId
            }

            return View(model);
        }




        public IActionResult PaymentSuccess(int bookingId)
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // Fetch the payment details for the booking (including PaymentStatus)
            var paymentDetails = GetPaymentDetails(bookingId, userId.Value); // Custom method to get full payment details for the booking

            if (paymentDetails == null)
            {
                TempData["Error"] = "Payment details not found.";
                return RedirectToAction("Error", "Home");
            }

            // Create the view model to display payment details
            var paymentViewModel = new PaymentViewModel
            {
                BookingId = bookingId,
                PaymentStatus = paymentDetails.PaymentStatus, // Set the payment status
                TransactionId = paymentDetails.TransactionId, // Set TransactionId
                Amount = paymentDetails.Amount, // Set the payment amount
                PaymentDate = paymentDetails.PaymentDate // Set the payment date
            };

            return View(paymentViewModel); // Pass the payment details and status to the view
        }

        // Method to get payment details from the database
        private PaymentDetails GetPaymentDetails(int bookingId, int userId)
        {
            PaymentDetails paymentDetails = null;

            using (SqlConnection con = new SqlConnection(_configuration.GetConnectionString("IEOConnection")))
            {
                SqlCommand cmd = new SqlCommand("SELECT TOP 1 PaymentStatus, TransactionId, Amount, PaymentDate FROM Payments WHERE BookingId = @BookingId AND UserId = @UserId ORDER BY PaymentDate DESC", con);
                cmd.Parameters.AddWithValue("@BookingId", bookingId);
                cmd.Parameters.AddWithValue("@UserId", userId);

                con.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        paymentDetails = new PaymentDetails
                        {
                            PaymentStatus = reader["PaymentStatus"].ToString(),
                            TransactionId = reader["TransactionId"].ToString(),
                            Amount = Convert.ToDecimal(reader["Amount"]),
                            PaymentDate = Convert.ToDateTime(reader["PaymentDate"])
                        };
                    }
                }
            }

            return paymentDetails;
        }






        // Method to update the user profile
        public void UpdateUserProfile(int userId, string firstName, string lastName, string email)
        {
            using (var connection = GetConnection())
            {
                connection.Open();
                using (var command = new SqlCommand("SP_UpdateUserProfile", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@UserId", userId);
                    command.Parameters.AddWithValue("@FirstName", firstName);
                    command.Parameters.AddWithValue("@LastName", lastName);
                    command.Parameters.AddWithValue("@Email", email);

                    command.ExecuteNonQuery(); // Execute the query
                }
            }
        }

        // Method to validate user password
        //public bool ValidatePassword(int userId, string currentPassword)
        //{
        //    using (var connection = GetConnection())
        //    {
        //        connection.Open();
        //        using (var command = new SqlCommand("SP_ValidatePassword", connection))
        //        {
        //            command.CommandType = CommandType.StoredProcedure;
        //            command.Parameters.AddWithValue("@UserId", userId);
        //            command.Parameters.AddWithValue("@Password", currentPassword);

        //            var result = (int)command.ExecuteScalar(); // Get the result
        //            return result == 1; // Return true if password is valid
        //        }
        //    }
        //}

        public bool ValidatePassword(int userId, string currentPassword)
        {
            // Base64 encode the entered password
            string encodedPassword = EncodePasswordToBase64(currentPassword);

            using (var connection = GetConnection())
            {
                connection.Open();
                using (var command = new SqlCommand("SP_ValidatePassword", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@UserId", userId);
                    command.Parameters.AddWithValue("@Password", encodedPassword); // Use encoded password for comparison

                    var result = (int)command.ExecuteScalar(); // Get the result
                    return result == 1; // Return true if password is valid
                }
            }
        }



        // Method to update the user password
        public void UpdateUserPassword(int userId, string newPassword)
        {
            using (var connection = GetConnection())
            {
                connection.Open();
                using (var command = new SqlCommand("SP_UpdateUserPassword", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@UserId", userId);
                    command.Parameters.AddWithValue("@NewPassword", newPassword);

                    command.ExecuteNonQuery(); // Execute the query to update password
                }
            }
        }


        [HttpGet]
        public IActionResult UserProfile()
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null || userId == 0)
            {
                return RedirectToAction("Index", "Login");
            }

            // Fetch user data (profile)
            UserViewModel model = new UserViewModel();
            using (var con = GetConnection())
            {
                SqlCommand cmd = new SqlCommand("SP_GetUserProfile", con)
                {
                    CommandType = CommandType.StoredProcedure
                };
                cmd.Parameters.AddWithValue("@UserId", userId);

                con.Open();
                using (SqlDataReader rdr = cmd.ExecuteReader())
                {
                    if (rdr.Read())
                    {
                        model.UserId = Convert.ToInt32(rdr["UserId"]);
                        model.FirstName = rdr["FirstName"].ToString();
                        model.LastName = rdr["LastName"].ToString();
                        model.Email = rdr["Email"].ToString();
                    }
                }
            }

            return View(model);
        }

        [HttpPost]
        public IActionResult UpdateProfile(UserViewModel model)
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null || userId == 0)
            {
                return RedirectToAction("Index", "Login");
            }

            

            if (!string.IsNullOrEmpty(model.NewPassword))
            {
                if (model.NewPassword != model.ConfirmNewPassword)
                {
                    ModelState.AddModelError("", "New password and confirm password do not match.");
                    return View("UserProfile", model);
                }

                // Validate current password
                if (!ValidatePassword(userId.Value, model.CurrentPassword))
                {
                    ModelState.AddModelError("", "Current password is incorrect.");
                    return View("UserProfile", model);
                }

                // Encode the new password (for storage)
                string encodedPassword = EncodePasswordToBase64(model.NewPassword);


                // Update the password in the database
                UpdateUserPassword(userId.Value, encodedPassword);
            }

            // Update the rest of the profile data (FirstName, LastName, Email)
            UpdateUserProfile(userId.Value, model.FirstName, model.LastName, model.Email);

            return RedirectToAction("Dashboard");
        }


        private string EncodePasswordToBase64(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
            {
                throw new ArgumentException("Password cannot be null or empty", nameof(password));
            }

            byte[] passwordBytes = Encoding.UTF8.GetBytes(password);
            return Convert.ToBase64String(passwordBytes);
        }



        public IActionResult PaymentL()
        {
            List<PaymentListViewModel> payments = new List<PaymentListViewModel>(); // List to store all payments

            try
            {
                using (var con = GetConnection())  // Assuming GetConnection() returns an open SQL connection
                {
                    con.Open();
                    SqlCommand cmd = new SqlCommand("SP_GetAllPayments", con)
                    {
                        CommandType = CommandType.StoredProcedure
                    };

                    using (SqlDataReader rdr = cmd.ExecuteReader())
                    {
                        while (rdr.Read())
                        {
                            payments.Add(new PaymentListViewModel
                            {
                                PaymentId = (int)rdr["PaymentId"],
                                UserName = rdr["UserName"].ToString(),
                                EventName = rdr["EventName"].ToString(),
                                Amount = Convert.ToDecimal(rdr["Amount"]),
                                PaymentDate = rdr["PaymentDate"] as DateTime?,  // Handling nullable DateTime
                                PaymentStatus = rdr["PaymentStatus"].ToString(),
                                PaymentMethod = rdr["PaymentMethod"].ToString(),
                                CardNumber = rdr["CardNumber"].ToString(),
                                PaymentType = rdr["PaymentType"].ToString(),
                                TransactionId = rdr["TransactionId"].ToString()
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Log the exception (replace with your logging mechanism)
                _logger.LogError(ex, "An error occurred while fetching payments.");
                ViewBag.ErrorMsg = "An error occurred while fetching payments. Please try again later.";
            }

            return View(payments); // Return the list of payments to the view
        }













    } // End of controller
}




