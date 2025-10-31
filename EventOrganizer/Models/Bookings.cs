using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.Extensions.Logging;

namespace EventOrganizer.Models
{
    public class Bookings
    {

        public Bookings()
        {
            BookingList = new List<Bookings>();
        }

       




        public List<Bookings> BookingList { get; set; }

        [Key]
        public int? BookingId { get; set; } // Primary Key (Auto-increment)

        [Required(ErrorMessage = "Please enter the User ID")]
        public int? UserId { get; set; } // Allow nullable to prevent conversion errors

        [Required(ErrorMessage = "Please enter the Event Title")]
        [StringLength(50, ErrorMessage = "Event title cannot exceed 50 characters.")]
        public string EventName { get; set; } = string.Empty; // Default empty string to prevent NULL errors

        [Required(ErrorMessage = "Please enter the Location")]
        [StringLength(50, ErrorMessage = "Location cannot exceed 50 characters.")]
        public string Location { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please enter the Venue Name")]
        [StringLength(50, ErrorMessage = "Venue name cannot exceed 50 characters.")]
        public string VenueName { get; set; } = string.Empty;

        [StringLength(50, ErrorMessage = "Service details cannot exceed 50 characters.")]
        public string? Services { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please enter the Capacity")]
        [Range(1, int.MaxValue, ErrorMessage = "Capacity must be a positive number.")]
        public int Capacity { get; set; }

        public string? Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please enter the Booking Date")]
        public DateTime EventDate { get; set; } // Allow nullable to prevent conversion errors

        public string? BookingStatus { get; set; } = "Pending";

        public string PaymentStatus { get; set; }

    }
}



