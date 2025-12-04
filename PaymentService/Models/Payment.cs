namespace PaymentService.Models
{
    public class Payment
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public string CustomerEmail { get; set; } = "";
        public decimal Amount { get; set; }
        public bool Success { get; set; }
        public string Status { get; set; } = "Pending"; // Pending, Success, Failed
        public string Message { get; set; } = "";
        public DateTime ProcessedAt { get; set; }
        public int RetryCount { get; set; } = 0;
    }
}
