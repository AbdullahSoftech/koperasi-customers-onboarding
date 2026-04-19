using Application.Commands.Customers;
using FluentValidation;

namespace Application.Validators.Customers;

public class ConfirmPinCommandValidator : AbstractValidator<ConfirmPinCommand>
{
    public ConfirmPinCommandValidator()
    {
        RuleFor(x => x.CustomerId)
            .NotEmpty().WithMessage("Customer ID is required.");

        RuleFor(x => x.Pin)
            .NotEmpty().WithMessage("PIN is required.")
            .Length(6).WithMessage("PIN must be exactly 6 digits.")
            .Matches("^[0-9]{6}$").WithMessage("PIN must contain digits only.");
    }
}
