/*using Microsoft.AspNetCore.Mvc;
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
        private readonly ILogger<OrdersController> _log;
        public OrdersController(OrdersDbContext db, RabbitMqPublisher publisher, ILogger<OrdersController> log)
        {
            _db = db; _publisher = publisher; _log = log;
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Order order)
        {
            if (order == null) return BadRequest();
            try
            {
                _db.Orders.Add(order);
                await _db.SaveChangesAsync();

                var evt = new OrderCreatedEvent
                {
                    OrderId = order.Id,
                    CustomerEmail = order.CustomerEmail,
                    Product = order.Product,
                    Amount = order.Amount
                };

                _publisher.PublishOrderCreated(evt, "order.created");

                _log.LogInformation("Order {id} created and event published", order.Id);
                return CreatedAtAction(nameof(Get), new { id = order.Id }, order);
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Error creating order");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id)
        {
            var o = await _db.Orders.FindAsync(id);
            if (o == null) return NotFound();
            return Ok(o);
        }
    }
}
*/