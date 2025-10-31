using System.ComponentModel.DataAnnotations;

namespace EventOrganizer.Models
{
    public class PaymentViewModel
    {


        
        
            public int BookingId { get; set; }

            [Required]
            public decimal Amount { get; set; }

            [Required]
            public string PaymentMethod { get; set; }
            public decimal FullPaymentAmount { get; set; }

           [Required]
            public string PaymentType { get; set; }

            [Required]
            [StringLength(16, MinimumLength = 16)]
            public string CardNumber { get; set; }

           [Required]
           public string ExpiryDate { get; set; }

        [Required]
            [StringLength(3, MinimumLength = 3)]
            public string CVV { get; set; }

            public DateTime PaymentDate { get; set; }

            public string? PaymentStatus { get; set; } 
            public string? TransactionId { get; set; }


    }


}
