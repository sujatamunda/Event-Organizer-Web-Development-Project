using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EventOrganizer.Models
{
    public class Services
    {


        public Services()
        {
            ServiceList = new List<Services>();
        }

        public List<Services> ServiceList { get; set; }

        [Key]
        public int? ServiceId { get; set; }

        [Required(ErrorMessage = "Please enter the service name")]
        [StringLength(50, ErrorMessage = "Service name cannot exceed 50 characters")]
        public string ServiceName { get; set; }


        //public string Description { get; set; }

        [Display(Name = "Description")]
        [DataType(DataType.MultilineText)]
        public string? Description { get; set; }

        public string? ImageUrl { get; set; }
        public decimal Cost { get; set; }

        [NotMapped]
        public IFormFile ImageFile { get; set; }

        [NotMapped]
        public string ExistingImageUrl { get; set; }


    }
}
