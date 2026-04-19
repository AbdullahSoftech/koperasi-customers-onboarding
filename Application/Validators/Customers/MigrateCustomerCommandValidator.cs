using Application.Commands.Customers;
using FluentValidation;

namespace Application.Validators.Customers;

public class MigrateCustomerCommandValidator : AbstractValidator<MigrateCustomerCommand>
{
    public MigrateCustomerCommandValidator()
    {
        RuleFor(x => x.OtpRequestId)
            .NotEmpty().WithMessage("OTP request ID is required.");

        RuleFor(x => x.PhoneNumber)
            .NotEmpty().WithMessage("Phone number is required.")
            .Matches(@"^\+?[0-9]{10,15}$").WithMessage("Phone number must be 10–15 digits.");

        RuleFor(x => x.OldSystemRef)
            .MaximumLength(100).WithMessage("Old system reference must not exceed 100 characters.")
            .When(x => !string.IsNullOrEmpty(x.OldSystemRef));
    }
}
