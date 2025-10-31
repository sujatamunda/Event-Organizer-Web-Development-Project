using System.Data.SqlClient;
using System.Data;
using Microsoft.AspNetCore.Mvc;
using EventOrganizer.Models;

namespace EventOrganizer.Controllers
{
    public class ExploreController : Controller
    {

        public readonly ILogger<ExploreController> _logger;
        private readonly IConfiguration _configuration;


        public ExploreController(ILogger<ExploreController> logger, IConfiguration configuration)
        {

            _logger = logger;
            _configuration = configuration;
        }


        [Obsolete]
        private SqlConnection GetConnection()
        {
            return new SqlConnection(_configuration.GetConnectionString("IEOConnection"));
        }
        public IActionResult Index(string venueSearch, string serviceSearch, string locationFilter)
        {
            var venues = GetVenues(venueSearch, locationFilter);
            var services = GetServices(serviceSearch);

            var model = new ExploreViewModel
            {
                VenueSearch = venueSearch,
                ServiceSearch = serviceSearch,
                LocationFilter = locationFilter,
                Venues = venues,  // Populate venues
                Services = services  // Populate services
            };

            return View(model);
        }






        public List<Venues> GetVenues(string searchQuery, string locationFilter)
        {
            var venues = new List<Venues>();

            using (var connection = GetConnection())

            {
                connection.Open();
                using (var command = new SqlCommand("SP_GetVenuesWithFilters", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;

                    // Add parameters to the stored procedure
                    command.Parameters.AddWithValue("@SearchQuery", string.IsNullOrEmpty(searchQuery) ? DBNull.Value : (object)searchQuery);
                    command.Parameters.AddWithValue("@LocationFilter", string.IsNullOrEmpty(locationFilter) ? DBNull.Value : (object)locationFilter);

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            venues.Add(new Venues
                            {
                                VenueId = reader.GetInt32(0),
                                VenueName = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),  // Check for DBNull
                                Location = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),   // Check for DBNull
                                Capacity = reader.IsDBNull(3) ? 0 : reader.GetDecimal(3),  // Handle DBNull for Capacity
                                ImageUrl = reader.IsDBNull(4) ? string.Empty : reader.GetString(4) , // Check for DBNull
                                Cost = reader.IsDBNull(5) ? 0 : reader.GetDecimal(5)
                            });
                        }
                    }
                }
            }

            return venues;
        }



        public List<Services> GetServices(string searchQuery)
        {
            var services = new List<Services>();

            using (var connection = GetConnection())

            {
                connection.Open();
                using (var command = new SqlCommand("SP_GetServicesWithSearch", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;

                    // Add search parameter to the stored procedure
                    command.Parameters.AddWithValue("@SearchQuery", string.IsNullOrEmpty(searchQuery) ? DBNull.Value : (object)searchQuery);

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            services.Add(new Services
                            {
                                ServiceId = reader.GetInt32(0),
                                ServiceName = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),  // Check for DBNull
                                Description = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),  // Check for DBNull
                                ImageUrl = reader.IsDBNull(3) ? string.Empty : reader.GetString(3) ,  // Check for DBNull
                                Cost = reader.IsDBNull(4) ? 0 : reader.GetDecimal(4)
                            });
                        }
                    }
                }
            }

            return services;
        }



        [HttpGet]
        public IActionResult GetFilteredVenuesAndServices(string venueSearch, string serviceSearch, string locationFilter)
        {
            var venues = GetVenues(venueSearch, locationFilter);
            var services = GetServices(serviceSearch);

            var model = new ExploreViewModel
            {
                Venues = venues,
                Services = services
            };

            // Return partial view with updated data
            return PartialView("VenuesAndServicesList", model);
        }


    }
}
