using Microsoft.EntityFrameworkCore;
using PaymentService.Data;
using PaymentService.Interfaces;
using PaymentService.Models;

namespace PaymentService.Repositories
{
    public class PaymentRepository : IPaymentRepository
    {
        private readonly PaymentsDbContext _context;
        private readonly ILogger<PaymentRepository> _logger;

        public PaymentRepository(PaymentsDbContext context, ILogger<PaymentRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<Payment> CreatePaymentAsync(Payment payment)
        {
            try
            {
                _context.Payments.Add(payment);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Payment record created for Order {OrderId}", payment.OrderId);
                return payment;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating payment record for Order {OrderId}", payment.OrderId);
                throw;
            }
        }

        public async Task<Payment?> GetPaymentByOrderIdAsync(int orderId)
        {
            try
            {
                return await _context.Payments
                    .FirstOrDefaultAsync(p => p.OrderId == orderId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving payment for Order {OrderId}", orderId);
                throw;
            }
        }

        public async Task<IEnumerable<Payment>> GetAllPaymentsAsync()
        {
            try
            {
                return await _context.Payments
                    .OrderByDescending(p => p.ProcessedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all payments");
                throw;
            }
        }

        public async Task UpdatePaymentAsync(Payment payment)
        {
            try
            {
                _context.Payments.Update(payment);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Payment record updated for Order {OrderId}", payment.OrderId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating payment for Order {OrderId}", payment.OrderId);
                throw;
            }
        }
    }
}