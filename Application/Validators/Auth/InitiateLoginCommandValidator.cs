using Application.Commands.Auth;
using FluentValidation;

namespace Application.Validators.Auth;

public class InitiateLoginCommandValidator : AbstractValidator<InitiateLoginCommand>
{
    public InitiateLoginCommandValidator()
    {
        RuleFor(x => x.IcNumber)
            .NotEmpty().WithMessage("IC number is required.")
            .MaximumLength(20).WithMessage("IC number must not exceed 20 characters.");
    }
}
