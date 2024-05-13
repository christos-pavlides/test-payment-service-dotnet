namespace PaymentService;

public enum ChargesBearer
{
     Originator, 
     Beneficiary, 
     Shared
}

public class Payment
{
     public int Id { get; set; }
     public double Amount { get; set; }
     public string PaymentCurrency { get; set; }
     
     public int OriginatorId { get; set; }
     public Contact Originator { get; set; }
     public int BeneficiaryId { get; set; }
     public Contact Beneficiary { get; set; }
     public ChargesBearer ChargesBearer { get; set; }
     public string Details { get; set; }
     public string? ReferenceNumber { get; set; }
}