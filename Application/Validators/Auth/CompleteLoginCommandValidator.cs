using Application.Commands.Auth;
using FluentValidation;

namespace Application.Validators.Auth;

public class CompleteLoginCommandValidator : AbstractValidator<CompleteLoginCommand>
{
    public CompleteLoginCommandValidator()
    {
        RuleFor(x => x.LoginSessionId)
            .NotEmpty().WithMessage("Login session ID is required.");

        RuleFor(x => x.CustomerId)
            .NotEmpty().WithMessage("Customer ID is required.");

        RuleFor(x => x.Pin)
            .NotEmpty().WithMessage("PIN is required.")
            .Length(6).WithMessage("PIN must be exactly 6 digits.")
            .Matches("^[0-9]{6}$").WithMessage("PIN must contain digits only.");
    }
}
