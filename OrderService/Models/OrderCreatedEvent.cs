namespace OrderService.Models
{
    public class OrderCreatedEvent
    {
        public int OrderId { get; set; }
        public string CustomerEmail { get; set; } = "";
        public string Product { get; set; } = "";
        public decimal Amount { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
