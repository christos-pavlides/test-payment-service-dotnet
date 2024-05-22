namespace PaymentService.Contracts;

public interface ITimeStampedModel
{
    DateTime CreatedAt { get; set; }
    DateTime UpdatedAt { get; set; }
}