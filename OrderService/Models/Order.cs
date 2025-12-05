namespace OrderService.Models
{
    public class Order
    {
        public int Id { get; set; }
        public string CustomerEmail { get; set; } = "";
        public string Product { get; set; } = "";
        public decimal Amount { get; set; }
        public string Status { get; set; } = "Pending"; // Pending, PaymentProcessed, PaymentFailed
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}
