using System.ComponentModel.DataAnnotations;

namespace EventOrganizer.Models
{
    public class LoginRequest
    {


        [Required(ErrorMessage = "Please enter the Email")]
        [RegularExpression(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", ErrorMessage = "Invalid email format.")]
        public string Email { get; set; }
        [Required(ErrorMessage = "Please enter the Password")]
        public string Password { get; set; }



    }
}
