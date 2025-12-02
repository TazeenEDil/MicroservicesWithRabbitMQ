namespace NotificationService.Models
{
    public class PaymentCompletedEvent
    {
     public int OrderId { get; set; }
     public string CustomerEmail { get; set; } = "";
     public decimal Amount { get; set; }
     public bool Success { get; set; }
     public string Message { get; set; } = "";
     public DateTime ProcessedAt { get; set; }
    }
}