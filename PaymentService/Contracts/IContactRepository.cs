using PaymentService.Models;

namespace PaymentService.Contracts;

public interface IContactRepository
{
    Task<Contact?> GetContactByNameAsync(string name);
    Task<Contact?> GetContactByIdAsync(int id);
    Task AddContactAsync(Contact contact);
    Task UpdateContactAsync(Contact contact);
}