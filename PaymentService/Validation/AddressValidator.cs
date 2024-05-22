using FluentValidation;

namespace PaymentService;

public class AddressValidator : AbstractValidator<Address>
{
    public AddressValidator()
    {
        RuleFor(a => a.AddressLine1).NotEmpty();
        RuleFor(a => a.CountryCode).NotEmpty();
    }
}