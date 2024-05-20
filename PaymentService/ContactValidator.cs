using FluentValidation;
using PaymentService.Models;

namespace PaymentService;

public class ContactValidator : AbstractValidator<Contact>
{
    public ContactValidator()
    {
        RuleFor(m => m.Name).NotEmpty();
        RuleFor(m => m.Address).SetValidator(new AddressValidator());
        RuleFor(m => m.Account).SetValidator(new BankAccountValidator());
    }
}