using Application.Commands.Customers;
using FluentValidation;

namespace Application.Validators.Customers;

public class RecordConsentCommandValidator : AbstractValidator<RecordConsentCommand>
{
    public RecordConsentCommandValidator()
    {
        RuleFor(x => x.CustomerId)
            .NotEmpty().WithMessage("Customer ID is required.");

        RuleFor(x => x.PolicyVersion)
            .NotEmpty().WithMessage("Policy version is required.")
            .MaximumLength(20).WithMessage("Policy version must not exceed 20 characters.");

        RuleFor(x => x.IsAccepted)
            .Equal(true).WithMessage("Privacy policy must be accepted to proceed.");

        RuleFor(x => x.IpAddress)
            .MaximumLength(45).WithMessage("IP address must not exceed 45 characters.")
            .When(x => !string.IsNullOrEmpty(x.IpAddress));
    }
}
