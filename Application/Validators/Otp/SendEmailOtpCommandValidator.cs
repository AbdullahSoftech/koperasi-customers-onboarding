using Application.Commands.Otp;
using Domain.Enums;
using FluentValidation;

namespace Application.Validators.Otp;

public class SendEmailOtpCommandValidator : AbstractValidator<SendEmailOtpCommand>
{
    public SendEmailOtpCommandValidator()
    {
        RuleFor(x => x.CustomerId)
            .NotEmpty().WithMessage("Customer ID is required.");

        RuleFor(x => x.Purpose)
            .Must(p => p == OtpPurpose.RegistrationEmail || p == OtpPurpose.EmailVerification)
            .WithMessage("Purpose must be RegistrationEmail or EmailVerification.");

        RuleFor(x => x.LoginSessionId)
            .NotEmpty().WithMessage("Login session ID is required for EmailVerification.")
            .When(x => x.Purpose == OtpPurpose.EmailVerification);
    }
}
