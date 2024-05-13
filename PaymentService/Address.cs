namespace PaymentService;

public class Address
{
    public int Id { get; set; }
    public int ContactId { get; set; }
    // public Contact Contact { get; set; }
    public string AddressLine1 { get; set; }
    public string? AddressLine2 { get; set; }
    public string? AddressLine3 { get; set; }
    public string? City { get; set; }
    public string? PostalCode { get; set; }
    public string? CountryCode { get; set; }
}