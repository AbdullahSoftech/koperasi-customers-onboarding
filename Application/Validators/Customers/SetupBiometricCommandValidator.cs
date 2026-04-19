using Application.Commands.Customers;
using FluentValidation;

namespace Application.Validators.Customers;

public class SetupBiometricCommandValidator : AbstractValidator<SetupBiometricCommand>
{
    public SetupBiometricCommandValidator()
    {
        RuleFor(x => x.CustomerId)
            .NotEmpty().WithMessage("Customer ID is required.");

        RuleFor(x => x.BiometricToken)
            .NotEmpty().WithMessage("Biometric token is required.")
            .MaximumLength(500).WithMessage("Biometric token must not exceed 500 characters.");
    }
}
