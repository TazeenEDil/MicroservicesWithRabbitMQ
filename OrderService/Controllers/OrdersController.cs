using Microsoft.AspNetCore.Mvc;
using OrderService.Interfaces;
using OrderService.Models;
using OrderService.Services;

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
                return BadRequest(new { error = "Invalid order data", details = "Email, Product, and Amount are required" });
            }

            try
            {
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create order");
                return StatusCode(500, new { error = "Failed to create order", details = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetOrders()
        {
            try
            {
                _logger.LogInformation("Retrieving all orders");
                var orders = await _orderRepo.GetAllOrdersAsync();
                return Ok(orders);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve orders");
                return StatusCode(500, new { error = "Failed to retrieve orders" });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetOrder(int id)
        {
            try
            {
                _logger.LogInformation("Retrieving order {OrderId}", id);
                var order = await _orderRepo.GetOrderByIdAsync(id);

                if (order == null)
                {
                    _logger.LogWarning("Order {OrderId} not found", id);
                    return NotFound(new { error = $"Order {id} not found" });
                }

                return Ok(order);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve order {OrderId}", id);
                return StatusCode(500, new { error = "Failed to retrieve order" });
            }
        }

        [HttpGet("health")]
        public IActionResult Health()
        {
            return Ok(new { status = "healthy", service = "OrderService", timestamp = DateTime.UtcNow });
        }
    }
}