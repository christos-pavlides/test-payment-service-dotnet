using PaymentService.Contracts;

namespace PaymentService.Models;

public class Contact : ITimeStampedModel
{
    public int Id { get; set; }
    public string Name { get; set; }
    public Address Address { get; set; }
    public BankAccount Account { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}