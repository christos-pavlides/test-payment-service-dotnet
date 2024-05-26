using Microsoft.EntityFrameworkCore;
using PaymentService.Contracts;
using PaymentService.Models;

namespace PaymentService.Data;

public class ApiDbContext : DbContext
{
    public ApiDbContext(DbContextOptions<ApiDbContext> options)
        : base(options)
    {
    }

    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<Contact> Contacts => Set<Contact>();
    public DbSet<BankAccount> BankAccounts => Set<BankAccount>();
    public DbSet<Address> Addresses => Set<Address>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Payment>()
            .HasOne(p => p.Beneficiary);

        modelBuilder.Entity<Payment>()
            .HasOne(p => p.Originator);

        modelBuilder.Entity<Contact>()
            .HasOne(c => c.Address);

        modelBuilder.Entity<Address>().ToTable("Addresses");
        modelBuilder.Entity<BankAccount>().ToTable("BankAccounts");
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        var timestamp = DateTime.UtcNow;
        foreach (var entry in ChangeTracker.Entries<ITimeStampedModel>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = timestamp;
                entry.Entity.UpdatedAt = timestamp;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = timestamp;
            }
        }

        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        var timestamp = DateTime.UtcNow;
        foreach (var entry in ChangeTracker.Entries<ITimeStampedModel>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = timestamp;
                entry.Entity.UpdatedAt = timestamp;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = timestamp;
            }
        }

        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }
}