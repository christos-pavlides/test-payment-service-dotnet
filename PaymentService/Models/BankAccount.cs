namespace PaymentService.Models;

public class BankAccount
{
    public int Id { get; set; }
    public int ContactId { get; set; }
    public string AccountNumber { get; set; }
    public string Bic { get; set; }
    
}