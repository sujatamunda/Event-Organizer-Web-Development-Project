using EventOrganizer.Models;
using System.Data.SqlClient;
using System.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace EventOrganizer.Controllers
{
    public class AdminController : Controller
    {


        public readonly ILogger<AdminController> _logger;
        private readonly IConfiguration _configuration;

        public AdminController(ILogger<AdminController> logger, IConfiguration configuration)
        {

            _logger = logger;
            _configuration = configuration;
        }


        [Obsolete]
        private SqlConnection GetConnection()
        {
            return new SqlConnection(_configuration.GetConnectionString("IEOConnection"));
        }

        //Admin  DashBoard Connection
        public IActionResult Dashboard()
        {
            //var userSession = GetUserSession();
            //if (userSession.UserId == 0 || userSession.UserId == null)
            //{
            //    return RedirectToAction("Index", "Login");
            //}
            //return View();

            string? uid = HttpContext.Session.GetString("UserId");
            if (uid == null)
            {
                return RedirectToAction("Index", "Login");
            }
            AdminDashboardViewModel model = new AdminDashboardViewModel();

            using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("IEOConnection")))
            {
                using (SqlCommand cmd = new SqlCommand("SP_DashboardStats", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    conn.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            model.TotalPendingBookings = Convert.ToInt32(reader["TotalPendingBookings"]);
                            model.TotalApprovedBookings = Convert.ToInt32(reader["TotalApprovedBookings"]);
                            model.TotalRejectedBookings = Convert.ToInt32(reader["TotalRejectedBookings"]);
                            model.TotalCompletedBookings = Convert.ToInt32(reader["TotalCompletedBookings"]);
                            model.TotalServices = Convert.ToInt32(reader["TotalServices"]);
                        }
                    }
                }
            }

            return View(model); // Pass data to your view
        }


        public IActionResult Customizations()
        {
            return View();
        }

        //Admin Venues Connection

        [HttpGet]
        public IActionResult Venues()
        {
            var model = new Venues();
            return View(model);
        }
        //[HttpPost]

        //public IActionResult Venues(Venues venue)
        //{
        //    if (ModelState.IsValid)
        //    {
        //        try
        //        {
        //            string connStr = _configuration.GetConnectionString("IEOConnection");
        //            using (SqlConnection con = new SqlConnection(connStr))
        //            {
        //                con.Open();
        //                SqlCommand cmd = new SqlCommand("SP_Venues", con);
        //                cmd.CommandType = CommandType.StoredProcedure;

        //                cmd.Parameters.AddWithValue("@VenueName", venue.VenueName);

        //                cmd.Parameters.AddWithValue("@Location", venue.Location);
        //                cmd.Parameters.AddWithValue("@Capacity", venue.Capacity);


        //                int i = cmd.ExecuteNonQuery();
        //                if (i > 0)
        //                {
        //                    ViewBag.SuccessMsg = "Venue inserted Successfully.";
        //                    ModelState.Clear();
        //                    return View(new Venues());
        //                }
        //                else
        //                {
        //                    ViewBag.ErrorMsg = "Failed to insert venue.";
        //                    return View(venue);
        //                }
        //            }
        //        }
        //        catch (Exception ex)
        //        {
        //            ViewBag.ErrorMsg = ex.Message;
        //        }
        //    }
        //    return View(venue);
        //}

        [HttpPost]
        public IActionResult Venues(Venues venue)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // 1. Handle image upload
                    string uniqueFileName = null;
                    if (venue.ImageFile != null)
                    {
                        // Define the path to save the file
                        string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/Images/venues");

                        // Create folder if not exists
                        if (!Directory.Exists(uploadsFolder))
                        {
                            Directory.CreateDirectory(uploadsFolder);
                        }

                        // Unique file name to avoid overwriting
                        uniqueFileName = Guid.NewGuid().ToString() + "_" + venue.ImageFile.FileName;

                        // Full path for the file
                        string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                        // Save the file
                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            venue.ImageFile.CopyTo(fileStream);
                        }

                        // ✅ Set the ImageUrl property for DB insert
                        venue.ImageUrl = "/Images/venues/" + uniqueFileName;
                    }

                    // 2. Insert into DB using SP
                    string connStr = _configuration.GetConnectionString("IEOConnection");
                    using (SqlConnection con = new SqlConnection(connStr))
                    {
                        con.Open();
                        SqlCommand cmd = new SqlCommand("SP_Venues", con);
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.AddWithValue("@VenueName", venue.VenueName);
                        cmd.Parameters.AddWithValue("@Location", venue.Location);
                        cmd.Parameters.AddWithValue("@Capacity", venue.Capacity);
                        cmd.Parameters.AddWithValue("@ImageUrl", venue.ImageUrl ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@Cost", venue.Cost);

                        int i = cmd.ExecuteNonQuery();
                        if (i > 0)
                        {
                            ViewBag.SuccessMsg = "Venue inserted successfully.";
                            ModelState.Clear();
                            return View(new Venues());
                        }
                        else
                        {
                            ViewBag.ErrorMsg = "Failed to insert venue.";
                        }
                    }
                }
                catch (Exception ex)
                {
                    ViewBag.ErrorMsg = ex.Message;
                }
            }

            return View(venue);
        }




        public IActionResult VenueList()
        {
            List<Venues> venues = new List<Venues>(); // List to store all venues

            try
            {
                using (var con = GetConnection())
                {
                    con.Open();
                    SqlCommand cmd = new SqlCommand("SP_GetAllVenues", con)
                    {
                        CommandType = CommandType.StoredProcedure
                    };

                    using (SqlDataReader rdr = cmd.ExecuteReader())
                    {
                        while (rdr.Read())
                        {
                            venues.Add(new Venues
                            {
                                VenueId = (int)rdr["VenueId"],
                                VenueName = rdr["VenueName"].ToString(),
                                Location = rdr["Location"].ToString(),
                                Capacity = Convert.ToDecimal(rdr["Capacity"]),
                                Cost = rdr["Cost"] == DBNull.Value ? 0 : Convert.ToDecimal(rdr["Cost"]),
                                ImageUrl = rdr["ImageUrl"] == DBNull.Value ? string.Empty : rdr["ImageUrl"].ToString(),
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Log the exception (replace with your logging mechanism)
                _logger.LogError(ex, "An error occurred while fetching venues.");
                ViewBag.ErrorMsg = "An error occurred while fetching venues. Please try again later.";
            }

            return View(venues); // Return the list of venues
        }

        public IActionResult VenueDelete(int id)
        {
            if (id <= 0)
            {
                return RedirectToAction("VenueList", "Admin");
            }

            try
            {
                using (var con = GetConnection())
                {
                    con.Open();
                    SqlCommand cmd = new SqlCommand("SP_DeleteVenue", con)
                    {
                        CommandType = CommandType.StoredProcedure
                    };
                    cmd.Parameters.AddWithValue("@VenueId", id);

                    int rowsAffected = cmd.ExecuteNonQuery();

                    if (rowsAffected > 0)
                    {
                        ViewBag.SuccessMsg = "Venue successfully deleted.";
                    }
                    else
                    {
                        ViewBag.ErrorMsg = "Error: Venue could not be deleted.";
                    }
                }
            }
            catch (Exception ex)
            {
                // Log the exception (replace _logger with your logging mechanism)
                _logger.LogError(ex, "An error occurred while deleting the venue.");
                ViewBag.ErrorMsg = "An error occurred while deleting the venue. Please try again later.";
            }

            return RedirectToAction("VenueList", "Admin");
        }




        [HttpGet]
        public IActionResult VenueEdit(int id)
        {
            Venues venue = new Venues();

            try
            {
                using (var con = GetConnection())
                {
                    con.Open();
                    SqlCommand cmd = new SqlCommand("SP_GetVenueById", con)
                    {
                        CommandType = CommandType.StoredProcedure
                    };
                    cmd.Parameters.AddWithValue("@VenueId", id);

                    using (SqlDataReader rdr = cmd.ExecuteReader())
                    {
                        if (rdr.Read())
                        {
                            venue.VenueId = (int)rdr["VenueId"];
                            venue.VenueName = rdr["VenueName"].ToString();
                            venue.Location = rdr["Location"].ToString();
                            venue.Capacity = Convert.ToDecimal(rdr["Capacity"]);
                            venue.Cost = Convert.ToDecimal(rdr["Cost"]); // Added Cost
                            venue.ImageUrl = rdr["ImageUrl"].ToString(); // Added ImageUrl
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Log error (replace with your logging mechanism)
                _logger.LogError(ex, "An error occurred while fetching venue data.");
                ViewBag.ErrorMsg = "An error occurred while fetching venue data. Please try again later.";
            }

            return View(venue);
        }


        [HttpPost]
        public IActionResult VenueEdit(Venues venue, string ExistingImageUrl)

        {
            ModelState.Remove("ImageFile");
            ModelState.Remove("ExistingImageUrl");


            if (ModelState.IsValid)
            {
                string imageUrl = venue.ImageUrl;

                if (venue.ImageFile != null && venue.ImageFile.Length > 0)
                {
                    var newFileName = Guid.NewGuid() + Path.GetExtension(venue.ImageFile.FileName);
                    var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/Images/venues", newFileName);

                    using (var fileStream = new FileStream(path, FileMode.Create))
                    {
                        venue.ImageFile.CopyTo(fileStream);
                    }

                    venue.ImageUrl = "/Images/venues/" + newFileName;
                }
                else
                {
                    // Preserve existing image
                    venue.ImageUrl = ExistingImageUrl;
                }

                using (var con = GetConnection())
                {
                    con.Open();
                    SqlCommand cmd = new SqlCommand("SP_UpdateVenue", con)
                    {
                        CommandType = CommandType.StoredProcedure
                    };
                    cmd.Parameters.AddWithValue("@VenueId", venue.VenueId);
                    cmd.Parameters.AddWithValue("@VenueName", venue.VenueName);
                    cmd.Parameters.AddWithValue("@Location", venue.Location);
                    cmd.Parameters.AddWithValue("@Capacity", venue.Capacity);
                    cmd.Parameters.AddWithValue("@Cost", venue.Cost);
                    cmd.Parameters.AddWithValue("@ImageUrl", venue.ImageUrl ?? (object)DBNull.Value);


                    cmd.ExecuteNonQuery();
                }

                return RedirectToAction("VenueList");
            }

            return View(venue);
        }



        [HttpGet]
        public IActionResult Events()
        {
            var model = new Events();
            return View(model);
        }

        [HttpPost]
        public IActionResult Events(Events eventModel)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    string connStr = _configuration.GetConnectionString("IEOConnection");
                    using (SqlConnection con = new SqlConnection(connStr))
                    {
                        con.Open();
                        SqlCommand cmd = new SqlCommand("SP_Events", con);
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.AddWithValue("@EventName", eventModel.EventName);
                        cmd.Parameters.AddWithValue("@Description", (object?)eventModel.Description ?? DBNull.Value);




                        int i = cmd.ExecuteNonQuery();
                        if (i > 0)
                        {
                            ViewBag.SuccessMsg = "Event inserted successfully.";
                            ModelState.Clear();
                            return View(new Events());
                        }
                        else
                        {
                            ViewBag.ErrorMsg = "Failed to insert event.";
                            return View(eventModel);
                        }
                    }
                }
                catch (Exception ex)
                {
                    ViewBag.ErrorMsg = ex.Message;
                }
            }
            return View(eventModel);
        }


        public IActionResult EventList()
        {
            List<Events> eventList = new List<Events>();

            try
            {
                using (var con = GetConnection())
                {
                    con.Open();
                    SqlCommand cmd = new SqlCommand("SP_GetAllEvents", con)
                    {
                        CommandType = CommandType.StoredProcedure
                    };

                    using (SqlDataReader rdr = cmd.ExecuteReader())
                    {
                        while (rdr.Read())
                        {
                            eventList.Add(new Events
                            {
                                EventId = Convert.ToInt32(rdr["EventId"]),
                                EventName = rdr["EventName"].ToString(),
                                Description = rdr["Description"].ToString(),
                               
                                
                                
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw;
            }

            return View(eventList); // Pass to the view
        }


        public IActionResult EventDelete(int id)
        {
            if (id > 0)
            {
                try
                {
                    using (var con = GetConnection())
                    {
                        con.Open();
                        SqlCommand cmd = new SqlCommand("SP_DeleteEvent", con)
                        {
                            CommandType = CommandType.StoredProcedure
                        };
                        cmd.Parameters.AddWithValue("@EventId", id);
                        cmd.ExecuteNonQuery();
                    }
                }
                catch (Exception ex)
                {
                    return RedirectToAction("EventList");
                }
            }

            return RedirectToAction("EventList");
        }



        [HttpGet]
        public IActionResult EventEdit(int id)
        {
            Events eventData = new Events();

            using (var con = GetConnection())
            {
                con.Open();
                SqlCommand cmd = new SqlCommand("SP_GetEventById", con)
                {
                    CommandType = CommandType.StoredProcedure
                };
                cmd.Parameters.AddWithValue("@EventId", id);

                using (SqlDataReader rdr = cmd.ExecuteReader())
                {
                    if (rdr.Read())
                    {
                        eventData.EventId = Convert.ToInt32(rdr["EventId"]);
                        eventData.EventName = rdr["EventName"].ToString();
                        eventData.Description = rdr["Description"].ToString();
                       
                        
                        
                    }
                }
            }

            return View(eventData); // Send data to edit form
        }



        [HttpPost]
        public IActionResult EventEdit(Events eventData)
        {
            if (ModelState.IsValid)
            {
                using (var con = GetConnection())
                {
                    con.Open();
                    SqlCommand cmd = new SqlCommand("SP_UpdateEvent", con)
                    {
                        CommandType = CommandType.StoredProcedure
                    };

                    cmd.Parameters.AddWithValue("@EventId", eventData.EventId);
                    cmd.Parameters.AddWithValue("@EventName", eventData.EventName);

                    if (!string.IsNullOrEmpty(eventData.Description))
                    {
                        cmd.Parameters.AddWithValue("@Description", eventData.Description);
                    }
                    else
                    {
                        cmd.Parameters.AddWithValue("@Description", DBNull.Value);
                    }





                    cmd.ExecuteNonQuery();
                }
                return RedirectToAction("EventList"); // Redirect after update
            }

            return View(eventData); // If invalid, stay on the form
        }











        public IActionResult UserList()
        {
            List<Register> users = new List<Register>();

            try
            {
                using (var con = GetConnection())
                {
                    con.Open();
                    SqlCommand cmd = new SqlCommand("SP_GetAllUser", con) // Your stored procedure
                    {
                        CommandType = CommandType.StoredProcedure
                    };

                    using (SqlDataReader rdr = cmd.ExecuteReader())
                    {
                        while (rdr.Read())
                        {
                            users.Add(new Register
                            {
                                UserId = Convert.ToInt32(rdr["UserId"]),
                                FirstName = rdr["FirstName"].ToString(),
                                LastName = rdr["LastName"].ToString(),
                                PhoneNumber = rdr["PhoneNumber"].ToString(),
                                Address = rdr["Address"].ToString(),
                                
                                Email = rdr["Email"].ToString(),
                                Status = rdr["Status"].ToString()


                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw;
            }

            return View(users); // Pass to the view
        }



        [HttpGet]
        public IActionResult ChangeStatus(int id, string status)
        {
            if (id > 0 && !string.IsNullOrEmpty(status))
            {
                using (SqlConnection con = new SqlConnection(_configuration.GetConnectionString("IEOConnection")))
                {
                    con.Open();
                    SqlCommand cmd = new SqlCommand("UPDATE [User] SET Status = @Status WHERE UserId = @UserId", con);
                    cmd.Parameters.AddWithValue("@Status", status);
                    cmd.Parameters.AddWithValue("@UserId", id);
                    cmd.ExecuteNonQuery();
                }
            }

            return RedirectToAction("UserList");
        }








        [HttpGet]
        public IActionResult Services()
        {
            var model = new Services();
            return View(model);
        }

        //[HttpPost]
        //public IActionResult Services(Services service)
        //{
        //    if (ModelState.IsValid)
        //    {
        //        try
        //        {
        //            string connStr = _configuration.GetConnectionString("IEOConnection");
        //            using (SqlConnection con = new SqlConnection(connStr))
        //            {
        //                con.Open();
        //                SqlCommand cmd = new SqlCommand("SP_Services", con); // Assuming your stored procedure is named SP_Services
        //                cmd.CommandType = CommandType.StoredProcedure;

        //                cmd.Parameters.AddWithValue("@ServiceName", service.ServiceName);
        //                cmd.Parameters.AddWithValue("@Description", service.Description);

        //                int i = cmd.ExecuteNonQuery();
        //                if (i > 0)
        //                {
        //                    ViewBag.SuccessMsg = "Service inserted Successfully.";
        //                    ModelState.Clear();
        //                    return View(new Services());
        //                }
        //                else
        //                {
        //                    ViewBag.ErrorMsg = "Failed to insert service.";
        //                    return View(service);
        //                }
        //            }
        //        }
        //        catch (Exception ex)
        //        {
        //            ViewBag.ErrorMsg = ex.Message;
        //        }
        //    }
        //    return View(service);
        //}


        [HttpPost]
        public IActionResult Services(Services service)
        {
            ModelState.Remove("ImageUrl");
            if (ModelState.IsValid)
            {
                try
                {
                    string uniqueFileName = null;

                    if (service.ImageFile != null)
                    {
                        string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/Images/services");
                        if (!Directory.Exists(uploadsFolder))
                            Directory.CreateDirectory(uploadsFolder);

                        uniqueFileName = Guid.NewGuid().ToString() + "_" + service.ImageFile.FileName;
                        string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            service.ImageFile.CopyTo(stream);
                        }

                        service.ImageUrl = "/Images/services/" + uniqueFileName;
                    }

                    string connStr = _configuration.GetConnectionString("IEOConnection");
                    using (SqlConnection con = new SqlConnection(connStr))
                    {
                        con.Open();
                        SqlCommand cmd = new SqlCommand("SP_Services", con);
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.AddWithValue("@ServiceName", service.ServiceName);
                        cmd.Parameters.AddWithValue("@Description", service.Description);
                        cmd.Parameters.AddWithValue("@ImageUrl", service.ImageUrl ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@Cost", service.Cost);

                        int i = cmd.ExecuteNonQuery();
                        if (i > 0)
                        {
                            ViewBag.SuccessMsg = "Service added successfully.";
                            ModelState.Clear();
                            return View(new Services());
                        }
                        else
                        {
                            ViewBag.ErrorMsg = "Service insertion failed.";
                        }
                    }
                }
                catch (Exception ex)
                {
                    ViewBag.ErrorMsg = ex.Message;
                }
            }

            return View(service);
        }


        public IActionResult ServiceList()
        {
            List<Services> services = new List<Services>(); // List to store all services

            try
            {
                using (var con = GetConnection())
                {
                    con.Open();
                    SqlCommand cmd = new SqlCommand("SP_GetAllServices", con)
                    {
                        CommandType = CommandType.StoredProcedure
                    };

                    using (SqlDataReader rdr = cmd.ExecuteReader())
                    {
                        while (rdr.Read())
                        {
                            services.Add(new Services
                            {
                                ServiceId = (int)rdr["ServiceId"],
                                ServiceName = rdr["ServiceName"].ToString(),
                                Description = rdr["Description"]?.ToString(),
                                Cost = rdr["Cost"] == DBNull.Value ? 0 : Convert.ToDecimal(rdr["Cost"]),
                                ImageUrl = rdr["ImageUrl"] == DBNull.Value ? string.Empty : rdr["ImageUrl"].ToString()
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Log the exception (replace with your logging mechanism)
                _logger.LogError(ex, "An error occurred while fetching services.");
                ViewBag.ErrorMsg = "An error occurred while fetching services. Please try again later.";
            }

            return View(services); // Return the list of services
        }




        public IActionResult ServiceDelete(int id)
        {
            if (id <= 0)
            {
                return RedirectToAction("ServiceList", "Admin");
            }

            try
            {
                using (var con = GetConnection())
                {
                    con.Open();
                    SqlCommand cmd = new SqlCommand("SP_DeleteService", con)
                    {
                        CommandType = CommandType.StoredProcedure
                    };
                    cmd.Parameters.AddWithValue("@ServiceId", id);

                    int rowsAffected = cmd.ExecuteNonQuery();

                    if (rowsAffected > 0)
                    {
                        ViewBag.SuccessMsg = "Service successfully deleted.";
                    }
                    else
                    {
                        ViewBag.ErrorMsg = "Error: Service could not be deleted.";
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while deleting the service.");
                ViewBag.ErrorMsg = "An error occurred while deleting the service. Please try again later.";
            }

            return RedirectToAction("ServiceList", "Admin");
        }


        [HttpGet]
        public IActionResult ServiceEdit(int id)
        {
            Services service = new Services();

            try
            {
                using (var con = GetConnection())
                {
                    con.Open();
                    SqlCommand cmd = new SqlCommand("SP_GetServiceById", con)
                    {
                        CommandType = CommandType.StoredProcedure
                    };
                    cmd.Parameters.AddWithValue("@ServiceId", id);

                    using (SqlDataReader rdr = cmd.ExecuteReader())
                    {
                        if (rdr.Read())
                        {
                            service.ServiceId = (int)rdr["ServiceId"];
                            service.ServiceName = rdr["ServiceName"].ToString();
                            service.Description = rdr["Description"].ToString();
                            service.Cost = Convert.ToDecimal(rdr["Cost"]);
                            service.ImageUrl = rdr["ImageUrl"].ToString();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while fetching service data.");
                ViewBag.ErrorMsg = "An error occurred while fetching service data. Please try again later.";
            }

            return View(service);
        }


        [HttpPost]
        public IActionResult ServiceEdit(Services service)
        {
            ModelState.Remove("ImageFile");
            ModelState.Remove("ExistingImageUrl");

            if (ModelState.IsValid)
            {
                if (service.ImageFile != null && service.ImageFile.Length > 0)
                {
                    var newFileName = Guid.NewGuid() + Path.GetExtension(service.ImageFile.FileName);
                    var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/Images/services", newFileName);

                    using (var fileStream = new FileStream(path, FileMode.Create))
                    {
                        service.ImageFile.CopyTo(fileStream);
                    }

                    service.ImageUrl = "/Images/services/" + newFileName;
                }
                else
                {
                    service.ImageUrl = service.ExistingImageUrl;
                }

                using (var con = GetConnection())
                {
                    con.Open();
                    SqlCommand cmd = new SqlCommand("SP_UpdateService", con)
                    {
                        CommandType = CommandType.StoredProcedure
                    };
                    cmd.Parameters.AddWithValue("@ServiceId", service.ServiceId);
                    cmd.Parameters.AddWithValue("@ServiceName", service.ServiceName);
                    cmd.Parameters.AddWithValue("@Description", string.IsNullOrEmpty(service.Description) ? (object)DBNull.Value : service.Description);

                    cmd.Parameters.AddWithValue("@Cost", service.Cost);
                    cmd.Parameters.AddWithValue("@ImageUrl", service.ImageUrl ?? (object)DBNull.Value);

                    cmd.ExecuteNonQuery();
                }

                return RedirectToAction("ServiceList");
            }

            return View(service);
        }

















        public IActionResult BookingList()
        {
            List<Bookings> bookingsList = new List<Bookings>();
            string connStr = _configuration.GetConnectionString("IEOConnection");

            using (SqlConnection con = new SqlConnection(connStr))
            {
                con.Open();
                SqlCommand cmd = new SqlCommand("SP_GetAllBookings", con); // Ensure the stored procedure exists
                cmd.CommandType = CommandType.StoredProcedure;

                SqlDataReader rdr = cmd.ExecuteReader();
                while (rdr.Read())
                {
                    bookingsList.Add(new Bookings
                    {
                        BookingId = rdr["BookingId"] != DBNull.Value ? Convert.ToInt32(rdr["BookingId"]) : (int?)null,
                        UserId = rdr["UserId"] != DBNull.Value ? Convert.ToInt32(rdr["UserId"]) : (int?)null,
                        EventName = rdr["EventName"] != DBNull.Value ? rdr["EventName"].ToString() : string.Empty,
                        Location = rdr["Location"] != DBNull.Value ? rdr["Location"].ToString() : string.Empty,
                        VenueName = rdr["Venues"] != DBNull.Value ? rdr["Venues"].ToString() : string.Empty,
                        Capacity = rdr["Capacity"] != DBNull.Value ? Convert.ToInt32(rdr["Capacity"]) : 0,
                        EventDate = rdr["EventDate"] != DBNull.Value ? Convert.ToDateTime(rdr["EventDate"]) : DateTime.MinValue,

                        BookingStatus = rdr["BookingStatus"] != DBNull.Value ? rdr["BookingStatus"].ToString() : "Pending",
                        PaymentStatus = rdr["PaymentStatus"] != DBNull.Value ? rdr["PaymentStatus"].ToString() : "Unpaid"
                    });
                }
            }

            return View(bookingsList);
        }







      

        public IActionResult CurrentBookings()
        {
            List<Bookings> bookingsList = new List<Bookings>();
            string connStr = _configuration.GetConnectionString("IEOConnection");

            using (SqlConnection con = new SqlConnection(connStr))
            {
                con.Open();
                SqlCommand cmd = new SqlCommand("SP_GetCurrentBookings", con);
                cmd.CommandType = CommandType.StoredProcedure;

                SqlDataReader rdr = cmd.ExecuteReader();
                while (rdr.Read())
                {
                    bookingsList.Add(new Bookings
                    {
                        // Handle DBNull for BookingId
                        BookingId = rdr["BookingId"] != DBNull.Value ? Convert.ToInt32(rdr["BookingId"]) : (int?)null,

                        // Handle DBNull for UserId
                        UserId = rdr["UserId"] != DBNull.Value ? Convert.ToInt32(rdr["UserId"]) : (int?)null,

                        // Handle DBNull for EventName
                        EventName = rdr["EventName"] != DBNull.Value ? rdr["EventName"].ToString() : string.Empty,

                        // Handle DBNull for Location
                        Location = rdr["Location"] != DBNull.Value ? rdr["Location"].ToString() : string.Empty,

                        // Handle DBNull for VenueName (not Venues)
                        VenueName = rdr["VenueName"] != DBNull.Value ? rdr["VenueName"].ToString() : string.Empty,

                        // Handle DBNull for Capacity
                        Capacity = rdr["Capacity"] != DBNull.Value ? Convert.ToInt32(rdr["Capacity"]) : 0,

                        // Handle DBNull for EventDate
                        EventDate = rdr["EventDate"] != DBNull.Value ? Convert.ToDateTime(rdr["EventDate"]) : DateTime.MinValue,

                        // Handle DBNull for BookingStatus
                        BookingStatus = rdr["BookingStatus"] != DBNull.Value ? rdr["BookingStatus"].ToString() : "Pending"
                    });
                }
            }

            return View(bookingsList);
        }








        [HttpGet]
        public IActionResult PendingBookings()
        {
            List<BookingViewModel> bookings = new List<BookingViewModel>();

            using (SqlConnection con = new SqlConnection(_configuration.GetConnectionString("IEOConnection")))
            {
                con.Open();
                SqlCommand cmd = new SqlCommand("SP_GetAllBookings", con); // Or a SP that gets only pending bookings
                cmd.CommandType = CommandType.StoredProcedure;

                using (SqlDataReader rdr = cmd.ExecuteReader())
                {
                    while (rdr.Read())
                    {
                        var booking = new BookingViewModel
                        {
                            BookingId = Convert.ToInt32(rdr["BookingId"]),
                            UserId = Convert.ToInt32(rdr["UserId"]),
                            EventName = rdr["EventName"].ToString(),
                            SelectedLocation = rdr["Location"].ToString(),
                            VenueName = rdr["Venues"].ToString(), // ✅ This matches your SP result


                            BookingStatus = rdr["BookingStatus"].ToString(),
                            EventDate = Convert.ToDateTime(rdr["EventDate"])
                            // Add more fields as needed
                        };

                        bookings.Add(booking);
                    }
                }
            }

            return View(bookings); // ✅ This now matches your view's @model List<BookingViewModel>
        }


        public IActionResult BookingHistory()
        {
            List<Bookings> bookingsList = new List<Bookings>();
            string connStr = _configuration.GetConnectionString("IEOConnection");

            try
            {
                using (SqlConnection con = new SqlConnection(connStr))
                {
                    con.Open();
                    using (SqlCommand cmd = new SqlCommand("SP_GetBookingHistory", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        using (SqlDataReader rdr = cmd.ExecuteReader())
                        {
                            while (rdr.Read())
                            {
                                bookingsList.Add(new Bookings
                                {
                                    BookingId = rdr["BookingId"] != DBNull.Value ? Convert.ToInt32(rdr["BookingId"]) : (int?)null,
                                    UserId = rdr["UserId"] != DBNull.Value ? Convert.ToInt32(rdr["UserId"]) : (int?)null,
                                    EventName = rdr["EventName"] != DBNull.Value ? rdr["EventName"].ToString() : string.Empty,
                                    Location = rdr["Location"] != DBNull.Value ? rdr["Location"].ToString() : string.Empty,

                                    // 🔥 Correct: Read VenueName not Venues
                                    VenueName = rdr["VenueName"] != DBNull.Value ? rdr["VenueName"].ToString() : string.Empty,

                                    Capacity = rdr["Capacity"] != DBNull.Value ? Convert.ToInt32(rdr["Capacity"]) : 0,

                                    EventDate = rdr["EventDate"] != DBNull.Value ? Convert.ToDateTime(rdr["EventDate"]) : DateTime.MinValue,
                                    BookingStatus = rdr["BookingStatus"] != DBNull.Value ? rdr["BookingStatus"].ToString() : "Pending"
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Optional: Log the exception somewhere
                ViewBag.ErrorMessage = "Error loading booking history.";
                return View(new List<Bookings>()); // Return empty list on error
            }

            return View(bookingsList);
        }











        // GET: UpdateBookingStatus (fetch booking details)
        [HttpGet]
        public IActionResult UpdateBookingStatus(int bookingId)
        {
            Bookings booking = new Bookings();

            try
            {
                using (var con = GetConnection())
                {
                    con.Open();
                    SqlCommand cmd = new SqlCommand("SP_GetBookingById", con); // Stored procedure to get booking details
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@BookingId", bookingId);

                    using (SqlDataReader rdr = cmd.ExecuteReader())
                    {
                        if (rdr.Read())
                        {
                            booking.BookingId = Convert.ToInt32(rdr["BookingId"]);
                            booking.UserId = Convert.ToInt32(rdr["UserId"]);
                            booking.EventName = rdr["EventName"].ToString();
                            booking.Location = rdr["Location"].ToString();
                            booking.VenueName = rdr["VenueBooked"].ToString();
                            booking.Services = rdr["Services"].ToString();
                            booking.Capacity = Convert.ToInt32(rdr["Capacity"]);
                            booking.Description = rdr["Description"].ToString();
                            booking.EventDate = Convert.ToDateTime(rdr["EventDate"]);
                            booking.BookingStatus = rdr["BookingStatus"].ToString();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMsg = "Error fetching booking details: " + ex.Message;
                return RedirectToAction("PendingBookings");
            }

            return View(booking); // Pass booking details to the view
        }

        // POST: UpdateBookingStatus (update booking status)
        [HttpPost]
        public IActionResult UpdateBookingStatus(int bookingId, string status)
        {
            string connStr = _configuration.GetConnectionString("IEOConnection");

            try
            {
                using (SqlConnection con = new SqlConnection(connStr))
                {
                    con.Open();
                    SqlCommand cmd = new SqlCommand("SP_UpdateBookingStatus", con);
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@BookingId", bookingId);
                    cmd.Parameters.AddWithValue("@Status", status);

                    cmd.ExecuteNonQuery();
                }

                TempData["SuccessMessage"] = "Booking status updated successfully!";

                // Redirect dynamically based on status
                switch (status)
                {
                    case "Pending":
                        return RedirectToAction("PendingBookings");
                    case "Approved":
                        return RedirectToAction("CurrentBookings");
                    case "Rejected":
                        return RedirectToAction("BookingHistory");
                    case "Completed":
                        return RedirectToAction("BookingList");
                    default:
                        return RedirectToAction("BookingList");
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "An error occurred while updating booking status: " + ex.Message;
                return RedirectToAction("BookingList");
            }
        }


        public IActionResult PaymentList()
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




        public IActionResult DeletePayment(int id)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(_configuration.GetConnectionString("IEOConnection")))
                {
                    con.Open();
                    using (SqlCommand cmd = new SqlCommand("DeletePaymentById", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@PaymentId", id);
                        cmd.ExecuteNonQuery();
                    }
                }
                TempData["SuccessMsg"] = "Payment deleted successfully.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMsg"] = "Error deleting payment: " + ex.Message;
            }
            return RedirectToAction("PaymentList");
        }



        public IActionResult ReviewList()
        {
            var reviews = new List<Reviews>();

            using (SqlConnection con = new SqlConnection(_configuration.GetConnectionString("IEOConnection")))
            {
                SqlCommand cmd = new SqlCommand("SELECT ReviewId, UserId, EventId, ReviewText, Rating, CreatedAt, VenueId, ServiceId FROM Reviews", con);
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

            return View(reviews);  // Pass the reviews to the view
        }

        [HttpPost]
        public IActionResult ApproveReview(int reviewId)
        {
            using (SqlConnection con = new SqlConnection(_configuration.GetConnectionString("IEOConnection")))
            {
                SqlCommand cmd = new SqlCommand("SP_ApproveReview", con);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@ReviewId", reviewId);

                con.Open();
                cmd.ExecuteNonQuery();
                con.Close();
            }

            // ✅ Return success response for AJAX
            return Json(new { success = true, message = "Review approved successfully." });
        }













    }
}
  
