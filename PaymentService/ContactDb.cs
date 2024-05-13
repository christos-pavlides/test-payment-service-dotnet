using Microsoft.EntityFrameworkCore;

namespace PaymentService;

class ContactDb : DbContext
{
    public ContactDb(DbContextOptions<ContactDb> options)
        : base(options) { }

    public DbSet<Contact> Contacts => Set<Contact>();
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Contact>()
            .HasOne(c => c.Address);
    }
}