using FluentValidation;
using PaymentService.Models;

namespace PaymentService;

public class PaymentValidator : AbstractValidator<Payment>
{
    public PaymentValidator()
    {
        RuleFor(  p => p.Amount).NotEmpty().WithMessage("Amount is required");
        RuleFor(p => p.Originator).SetValidator(new ContactValidator());
        RuleFor(p => p.Beneficiary).SetValidator(new ContactValidator());
        RuleFor(p => p.ChargesBearer).NotEmpty().WithMessage("Charges Bearer is required");
        RuleFor( p => p.PaymentCurrency).NotEmpty().WithMessage("Payment Currency is required");
        RuleFor(p => p.Details).NotEmpty().WithMessage("Details is required");
    }
}