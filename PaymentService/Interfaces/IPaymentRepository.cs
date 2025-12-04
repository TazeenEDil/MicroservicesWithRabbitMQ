using PaymentService.Models;

namespace PaymentService.Interfaces
{
    public interface IPaymentRepository
    {
        Task<Payment> CreatePaymentAsync(Payment payment);
        Task<Payment?> GetPaymentByOrderIdAsync(int orderId);
        Task<IEnumerable<Payment>> GetAllPaymentsAsync();
        Task UpdatePaymentAsync(Payment payment);
    }
}