using Application.Commands.Customers;
using FluentValidation;

namespace Application.Validators.Customers;

public class MigrateCustomerCommandValidator : AbstractValidator<MigrateCustomerCommand>
{
    public MigrateCustomerCommandValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Full name is required.")
            .MaximumLength(100).WithMessage("Full name must not exceed 100 characters.");

        RuleFor(x => x.PhoneNumber)
            .NotEmpty().WithMessage("Phone number is required.")
            .Matches(@"^\+?[0-9]{10,15}$").WithMessage("Phone number must be 10–15 digits.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Email is not valid.")
            .MaximumLength(150).WithMessage("Email must not exceed 150 characters.");

        RuleFor(x => x.NationalId)
            .NotEmpty().WithMessage("National ID (IC number) is required.")
            .Matches(@"^\d{8}$").WithMessage("National ID must be exactly 8 digits.");

        RuleFor(x => x.CustomerType)
            .IsInEnum().WithMessage("Invalid customer type.");

        RuleFor(x => x.OldSystemRef)
            .MaximumLength(100).WithMessage("Old system reference must not exceed 100 characters.")
            .When(x => !string.IsNullOrEmpty(x.OldSystemRef));

        RuleFor(x => x.Notes)
            .MaximumLength(500).WithMessage("Notes must not exceed 500 characters.")
            .When(x => !string.IsNullOrEmpty(x.Notes));
    }
}