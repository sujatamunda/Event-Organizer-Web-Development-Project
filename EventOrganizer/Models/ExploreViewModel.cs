using System;

namespace EventOrganizer.Models
{
    public class ExploreViewModel
    {
        public List<Venues> Venues { get; set; } = new List<Venues>(); // Direct list of venues
        public List<Services> Services { get; set; } = new List<Services>();

        public string VenueSearch { get; set; }
        public string ServiceSearch { get; set; }
        public string LocationFilter { get; set; }
    }
}
