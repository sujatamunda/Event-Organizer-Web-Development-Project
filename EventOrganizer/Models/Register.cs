using System.ComponentModel.DataAnnotations;

namespace EventOrganizer.Models
{
    public class Register
    {
        public Register()
        {
            RegisterList = new List<Register>();
        }

        public List<Register> RegisterList { get; set; }

        [Key]
        public int? UserId { get; set; }
        [Required(ErrorMessage = "Please enter the fist name")]
        public string FirstName { get; set; }
        [Required(ErrorMessage = "Please enter the Last name")]
        public string LastName { get; set; }
        [Required(ErrorMessage = "Please enter the Mobile Number")]
        [RegularExpression(@"^\d{10}$", ErrorMessage = "Invalid mobile number. It should be 10 digits.")]
        public string PhoneNumber { get; set; }
        [Required(ErrorMessage = "Please enter the Email")]
        [RegularExpression(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", ErrorMessage = "Invalid email format.")]
        public string Email { get; set; }
        [Required(ErrorMessage = "Please enter the Address")]
        public string Address { get; set; }
        
        

        public string? UserType { get; set; } 

        //[Required(ErrorMessage = "Please enter the Password")]
        public string  Password { get; set;}

        public string? Status { get; set; }

    }
}
