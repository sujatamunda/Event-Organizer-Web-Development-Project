using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace EventOrganizer.Models
{
    public class BookingViewModel
    {
        



        public int? BookingId { get; set; }
        public int? UserId { get; set; }

        [Required(ErrorMessage = "Enter Event Name")]
        public int? EventId { get; set; } // for dropdown binding

        [Required(ErrorMessage = "Enter Event Name")]
        public string EventName { get; set; } = string.Empty;
        public string? CustomEvent { get; set; }
       
        [Required(ErrorMessage = "Enter Location Name")]
        public string Location { get; set; } = string.Empty;
        public string? SelectedLocation { get; set; }
        public string? SelectedCity { get; set; }

        [Required(ErrorMessage = "Venue is required")]
        public int VenueId { get; set; } // Used for filtering and view logic

        public string? VenueName { get; set; } = string.Empty;
        public string? CustomVenue { get; set; }
        public string? ServiceName { get; set; }
        public List<string> SelectedServiceName { get; set; } = new List<string>();

        public string? CustomService { get; set; }
        public int Capacity { get; set; }
        public string? Description { get; set; }
        [Required(ErrorMessage = "Event Date is required")]
        [DataType(DataType.Date)]
        public DateTime EventDate { get; set; }

        
        public List<DateTime>? AvailableDates { get; set; }

        public string? BookingStatus { get; set; } = "Pending";

        public string? PaymentStatus { get; set; } = "Unpaid";

        public int BookingNo { get; set; }


        // Dropdown Lists for selection
        public List<SelectListItem> Events { get; set; }
        public List<SelectListItem> Locations { get; set; }
        public List<SelectListItem> Venues { get; set; }
        public List<SelectListItem> Services { get; set; }

        // Constructor to Initialize Lists
        public BookingViewModel()
        {
            Events = new List<SelectListItem>();
            Locations = new List<SelectListItem>();
            Venues = new List<SelectListItem>();
            Services = new List<SelectListItem>();
        }

    }
}