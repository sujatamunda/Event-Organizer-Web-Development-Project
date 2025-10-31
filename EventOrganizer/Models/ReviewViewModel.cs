using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;

namespace EventOrganizer.Models
{
    public class ReviewViewModel
    {
        public Reviews Review { get; set; }

        public List<SelectListItem> Users { get; set; }
        public List<SelectListItem> Events { get; set; }

        public List<SelectListItem> Venues { get; set; } 
        public List<SelectListItem> Services { get; set; }
    }
}
