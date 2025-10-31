using System.ComponentModel.DataAnnotations;

namespace EventOrganizer.Models
{
    public class LoginResponse
    {
        [Key]
        public int UserId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string PhoneNumber { get; set; }
        public string Address { get; set; }
        
        public string Password { get; set; }
        public string  UserType { get; set; }

        public string Email { get; set; }
    }
}
