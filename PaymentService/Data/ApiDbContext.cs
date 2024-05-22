using Microsoft.EntityFrameworkCore;
using PaymentService.Models;

namespace PaymentService.Data;

public class ApiDbContext : DbContext
{
    public ApiDbContext(DbContextOptions<ApiDbContext> options)
        : base(options) { }

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
}