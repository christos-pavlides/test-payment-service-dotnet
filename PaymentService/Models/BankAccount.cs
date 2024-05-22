using PaymentService.Contracts;

namespace PaymentService.Models;

public class BankAccount : ITimeStampedModel
{
    public int Id { get; set; }
    public int ContactId { get; set; }
    public string AccountNumber { get; set; }
    public string Bic { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}