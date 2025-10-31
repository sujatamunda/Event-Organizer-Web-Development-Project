using System.ComponentModel.DataAnnotations;

namespace EventOrganizer.Models
{
    public class Events
    {
        public Events()
        {
            EventList = new List<Events>();
        }

        public List<Events> EventList { get; set; }

        [Key]
        public int? EventId { get; set; }

        [Required(ErrorMessage = "Please enter the event name")]
        [StringLength(50, ErrorMessage = "Title cannot exceed 50 characters")]
        public string EventName { get; set; }

        
        public string ? Description { get; set; }


       

        

    }
}
