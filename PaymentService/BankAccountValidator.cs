using FluentValidation;
using PaymentService.Models;

namespace PaymentService;

public class BankAccountValidator : AbstractValidator<BankAccount>
{
    public BankAccountValidator()
    {
        RuleFor( m => m.AccountNumber).NotEmpty();
        RuleFor( m => m.Bic).NotEmpty();
    }
}