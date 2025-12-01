namespace NotificationService.Models
{
    public class PaymentCompletedEvent
    {
        public int OrderId { get; set; }
        public bool Success { get; set; }
        public string Reason { get; set; } = "";
    }
}
