using OrderService.Data;
using OrderService.Models;
using Microsoft.EntityFrameworkCore;
using OrderService.Interfaces;


namespace OrderService.Repositories
{
    public class OrderRepository : IOrderRepository
    {
        private readonly OrdersDbContext _context;
        private readonly ILogger<OrderRepository> _logger;

        public OrderRepository(OrdersDbContext context, ILogger<OrderRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<Order> CreateOrderAsync(Order order)
        {
            try
            {
                _context.Orders.Add(order);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Order {OrderId} created successfully in database", order.Id);
                return order;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating order in database");
                throw;
            }
        }

        public async Task<Order?> GetOrderByIdAsync(int id)
        {
            try
            {
                return await _context.Orders.FindAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving order {OrderId}", id);
                throw;
            }
        }

        public async Task<IEnumerable<Order>> GetAllOrdersAsync()
        {
            try
            {
                return await _context.Orders
                    .OrderByDescending(o => o.CreatedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all orders");
                throw;
            }
        }

        public async Task<bool> UpdateOrderStatusAsync(int orderId, string status)
        {
            try
            {
                var order = await _context.Orders.FindAsync(orderId);
                if (order == null)
                {
                    _logger.LogWarning("Order {OrderId} not found for status update", orderId);
                    return false;
                }

                order.Status = status;
                order.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Order {OrderId} status updated to {Status}", orderId, status);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating order {OrderId} status", orderId);
                throw;
            }

        }
    }
}
