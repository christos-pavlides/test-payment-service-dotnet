using Microsoft.EntityFrameworkCore;
using PaymentService.Data;
using PaymentService.Models;

namespace PaymentService.Repositories;

public class PaymentRepository : IPaymentRepository
{
    private readonly ApiDbContext _db;

    public PaymentRepository(ApiDbContext db)
    {
        _db = db;
    }

    public async Task AddPaymentAsync(Payment payment)
    {
        await _db.Payments.AddAsync(payment);
    }

    public async Task<Payment?> GetPaymentByIdAsync(int id)
    {
        return await _db.Payments.FindAsync(id);
    }
    
    public async Task<List<Payment>> GetPaymentsAsync(IEnumerable<int> ids, DateTime? fromDate, DateTime? toDate, double? minAmount, double? maxAmount)
    {
        IQueryable<Payment> payments = _db.Payments;

        if (ids != null && ids.Any())
        {
            payments = payments.Where(p => ids.Contains(p.Id));
        }

        if (fromDate.HasValue)
        {
            payments = payments.Where(p => p.CreatedAt >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            payments = payments.Where(p => p.CreatedAt <= toDate.Value);
        }

        if (minAmount.HasValue)
        {
            payments = payments.Where(p => p.Amount >= minAmount.Value);
        }

        if (maxAmount.HasValue)
        {
            payments = payments.Where(p => p.Amount <= maxAmount.Value);
        }

        return await payments
            .Include(p => p.Beneficiary.Address)
            .Include(p => p.Beneficiary.Account)
            .Include(p => p.Originator.Address)
            .Include(p => p.Originator.Account)
            .ToListAsync();
    }
}