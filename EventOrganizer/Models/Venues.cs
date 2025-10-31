using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EventOrganizer.Models
{
    public class Venues
    {
        public Venues() 
        {
            VenueList = new List<Venues>();
        }
        public List<Venues> VenueList { get; set; } 
        [Key]
        public int? VenueId { get; set; }

        [Required(ErrorMessage = "Please enter the Venue Name")]
        public string VenueName { get; set; }

       

        [Required(ErrorMessage = "Please enter the Location")]
        public string Location { get; set; }

        [Required(ErrorMessage = "Please enter the Capacity")]
        [Range(1, int.MaxValue, ErrorMessage = "Capacity must be a positive number")]
        public decimal Capacity { get; set; }

        public string? ImageUrl { get; set; }

        [NotMapped]
        public IFormFile? ImageFile { get; set; }


        public decimal Cost { get; set; }


    }
}
