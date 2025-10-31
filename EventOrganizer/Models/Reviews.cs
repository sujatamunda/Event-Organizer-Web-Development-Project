using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Logging;

namespace EventOrganizer.Models
{
    public class Reviews
    {


        public int ReviewId { get; set; }

        
        public int UserId { get; set; }

        
        public int EventId { get; set; }

        public int? VenueId { get; set; }
        public int? ServiceId { get; set; }


        [Required]
        [StringLength(1000)]
        public string ReviewText { get; set; }

        [Required]
        [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5.")]
        public int Rating { get; set; }

        public DateTime CreatedAt { get; set; }


    }

}

