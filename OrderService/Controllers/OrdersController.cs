using Microsoft.AspNetCore.Mvc;
using OrderService.Interfaces;
using OrderService.Models;

namespace OrderService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrdersController : ControllerBase
    {
        private readonly IOrderRepository _orderRepo;
        private readonly IRabbitMqPublisher _publisher;
        private readonly ILogger<OrdersController> _logger;

        public OrdersController(
            IOrderRepository orderRepo,
            IRabbitMqPublisher publisher,
            ILogger<OrdersController> logger)
        {
            _orderRepo = orderRepo;
            _publisher = publisher;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> CreateOrder([FromBody] Order order)
        {
            _logger.LogInformation("Received order creation request for {CustomerEmail}", order.CustomerEmail);

            if (string.IsNullOrEmpty(order.CustomerEmail) ||
                string.IsNullOrEmpty(order.Product) ||
                order.Amount <= 0)
            {
                _logger.LogWarning("Invalid order data received");
                throw new ArgumentException("Email, Product, and Amount are required. Amount must be greater than 0.");
            }

            order.CreatedAt = DateTime.UtcNow;
            order.Status = "Pending";

            var createdOrder = await _orderRepo.CreateOrderAsync(order);
            _logger.LogInformation("Order {OrderId} saved to database", createdOrder.Id);

            var evt = new OrderCreatedEvent
            {
                OrderId = createdOrder.Id,
                CustomerEmail = createdOrder.CustomerEmail,
                Product = createdOrder.Product,
                Amount = createdOrder.Amount,
                CreatedAt = createdOrder.CreatedAt
            };

            _publisher.PublishOrderCreated(evt);
            _logger.LogInformation("OrderCreatedEvent published for Order {OrderId}", createdOrder.Id);

            return Ok(new
            {
                message = "Order created successfully",
                order = createdOrder
            });
        }

        [HttpGet]
        public async Task<IActionResult> GetOrders()
        {
            _logger.LogInformation("Retrieving all orders");
            var orders = await _orderRepo.GetAllOrdersAsync();
            return Ok(orders);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetOrder(int id)
        {
            _logger.LogInformation("Retrieving order {OrderId}", id);
            var order = await _orderRepo.GetOrderByIdAsync(id);

            if (order == null)
            {
                _logger.LogWarning("Order {OrderId} not found", id);
                throw new KeyNotFoundException($"Order {id} not found");
            }

            return Ok(order);
        }

        [HttpGet("health")]
        public IActionResult Health()
        {
            return Ok(new { status = "healthy", service = "OrderService", timestamp = DateTime.UtcNow });
        }
    }
}