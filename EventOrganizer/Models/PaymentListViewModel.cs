namespace EventOrganizer.Models
{
    public class PaymentListViewModel
    {
        public int PaymentId { get; set; }
        public string UserName { get; set; }
        public string EventName { get; set; }
        public decimal Amount { get; set; }
        public DateTime? PaymentDate { get; set; }
        public string PaymentStatus { get; set; }
        public string PaymentMethod { get; set; }
        public string CardNumber { get; set; }
        public string PaymentType { get; set; }
        public string TransactionId { get; set; }
    }
}
