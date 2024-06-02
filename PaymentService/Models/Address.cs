using PaymentService.Contracts;

namespace PaymentService;

/// <summary>
/// Address
/// </summary>
public class Address : ITimeStampedModel
{
    /// <summary>
    /// Id of an address
    /// </summary>
    public int Id { get; set; }
    /// <summary>
    /// Contact id
    /// </summary>
    public int ContactId { get; set; }
    /// <summary>
    /// Address line 1
    /// </summary>
    // public Contact Contact { get; set; }
    public string AddressLine1 { get; set; }
    /// <summary>
    /// Address line 2
    /// </summary>
    public string? AddressLine2 { get; set; }
    /// <summary>
    /// Address line 3
    /// </summary>
    public string? AddressLine3 { get; set; }
    public string? City { get; set; }
    public string? PostalCode { get; set; }
    /// <summary>
    /// Country Code
    /// </summary>
    public string CountryCode { get; set; }
    /// <summary>
    /// Created at
    /// </summary>
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}