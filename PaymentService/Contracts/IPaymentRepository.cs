using PaymentService.Models;

namespace PaymentService.Repositories;

public interface IPaymentRepository
{
    Task AddPaymentAsync(Payment payment);
    Task<Payment?> GetPaymentByIdAsync(int id);
    Task<List<Payment>> GetPaymentsAsync(IEnumerable<int> ids, DateTime? fromDate, DateTime? toDate, double? minAmount, double? maxAmount);
}