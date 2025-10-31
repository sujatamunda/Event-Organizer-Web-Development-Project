namespace EventOrganizer.Models
{
    public class PaymentDetails
    {
        public string PaymentStatus { get; set; }
        public string TransactionId { get; set; }
        public decimal Amount { get; set; }
        public DateTime PaymentDate { get; set; }
    }
}
