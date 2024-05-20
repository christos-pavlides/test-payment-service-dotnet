namespace PaymentService.Models;

public class Contact
{
    public int Id { get; set; }
    public string Name { get; set; }
    public Address Address { get; set; }
    public BankAccount Account { get; set; }
}

