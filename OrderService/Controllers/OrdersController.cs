using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrderService.Data;
using OrderService.Models;
using OrderService.Services;

namespace OrderService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrdersController : ControllerBase
    {
        private readonly OrdersDbContext _db;
        private readonly RabbitMqPublisher _publisher;

        public OrdersController(OrdersDbContext db, RabbitMqPublisher publisher)
        {
            _db = db;
            _publisher = publisher;
        }

        [HttpPost]
        public async Task<IActionResult> CreateOrder([FromBody] Order order)
        {
            if (string.IsNullOrEmpty(order.CustomerEmail) ||
                string.IsNullOrEmpty(order.Product) ||
                order.Amount <= 0)
            {
                return BadRequest("Invalid order data");
            }

            order.CreatedAt = DateTime.UtcNow;
            _db.Orders.Add(order);
            await _db.SaveChangesAsync();

            var evt = new OrderCreatedEvent
            {
                OrderId = order.Id,
                CustomerEmail = order.CustomerEmail,
                Product = order.Product,
                Amount = order.Amount
            };

            _publisher.PublishOrderCreated(evt);

            return Ok(order);
        }

        [HttpGet]
        public async Task<IActionResult> GetOrders()
        {
            var orders = await _db.Orders.ToListAsync();
            return Ok(orders);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetOrder(int id)
        {
            var order = await _db.Orders.FindAsync(id);
            if (order == null) return NotFound();
            return Ok(order);
        }
    }
}