namespace EventOrganizer.Models
{
    public class ReviewTestimonial
    {
        public int ReviewId { get; set; }
        public string UserName { get; set; }
        public string ReviewText { get; set; }
        public int Rating { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
