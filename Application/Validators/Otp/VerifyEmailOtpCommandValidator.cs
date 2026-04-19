using Application.Commands.Otp;
using FluentValidation;

namespace Application.Validators.Otp;

public class VerifyEmailOtpCommandValidator : AbstractValidator<VerifyEmailOtpCommand>
{
    public VerifyEmailOtpCommandValidator()
    {
        RuleFor(x => x.CustomerId)
            .NotEmpty().WithMessage("Customer ID is required.");

        RuleFor(x => x.OtpRequestId)
            .NotEmpty().WithMessage("OTP request ID is required.");

        RuleFor(x => x.OtpCode)
            .NotEmpty().WithMessage("OTP code is required.")
            .Length(4).WithMessage("OTP code must be exactly 4 digits.")
            .Matches("^[0-9]{4}$").WithMessage("OTP code must contain digits only.");
    }
}
