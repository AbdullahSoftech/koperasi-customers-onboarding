using Application.Commands.Customers;
using FluentValidation;

namespace Application.Validators.Customers;

public class RegisterCustomerCommandValidator : AbstractValidator<RegisterCustomerCommand>
{
    public RegisterCustomerCommandValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Full name is required.")
            .MaximumLength(100).WithMessage("Full name must not exceed 100 characters.");

        RuleFor(x => x.IcNumber)
            .NotEmpty().WithMessage("IC number is required.")
            .MaximumLength(20).WithMessage("IC number must not exceed 20 characters.");

        RuleFor(x => x.MobileNumber)
            .NotEmpty().WithMessage("Mobile number is required.")
            .Matches(@"^\+?[0-9]{10,15}$").WithMessage("Mobile number must be 10–15 digits.");

        RuleFor(x => x.EmailAddress)
            .NotEmpty().WithMessage("Email address is required.")
            .EmailAddress().WithMessage("A valid email address is required.")
            .MaximumLength(100).WithMessage("Email must not exceed 100 characters.");
    }
}
