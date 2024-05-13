using Microsoft.EntityFrameworkCore;

namespace PaymentService;

public class PaymentDb : DbContext
{
    public PaymentDb(DbContextOptions<PaymentDb> options)
        : base(options) { }

    public DbSet<Payment> Payments => Set<Payment>();
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        modelBuilder.Entity<Payment>()
            .HasOne(p => p.Beneficiary);

        modelBuilder.Entity<Payment>()
            .HasOne(p => p.Originator);

    }
}