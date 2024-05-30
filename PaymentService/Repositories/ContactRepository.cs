using Microsoft.EntityFrameworkCore;
using PaymentService.Contracts;
using PaymentService.Data;
using PaymentService.Models;

namespace PaymentService.Repositories;

public class ContactRepository : IContactRepository
{
    private readonly ApiDbContext _db;

    public ContactRepository(ApiDbContext db)
    {
        _db = db;
    }

    public async Task<Contact?> GetContactByNameAsync(string name)
    {
        return await _db.Contacts.Include("Address").Include("Account")
            .FirstOrDefaultAsync(c => c.Name == name);
    }

    public async Task<Contact?> GetContactByIdAsync(int id)
    {
        return await _db.Contacts.Include("Address").Include("Account")
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task AddContactAsync(Contact contact)
    {
        await _db.Contacts.AddAsync(contact);
    }

    public async Task UpdateContactAsync(Contact contact)
    {
        _db.Contacts.Update(contact);
    }
}