using Application.Commands.Otp;
using FluentValidation;

namespace Application.Validators.Otp;

public class VerifyOtpCommandValidator : AbstractValidator<VerifyOtpCommand>
{
    public VerifyOtpCommandValidator()
    {
        RuleFor(x => x.OtpRequestId)
            .NotEmpty().WithMessage("OTP request ID is required.");

        RuleFor(x => x.PhoneNumber)
            .NotEmpty().WithMessage("Phone number is required.")
            .Matches(@"^\+?[0-9]{10,15}$").WithMessage("Phone number must be 10–15 digits.");

        RuleFor(x => x.OtpCode)
            .NotEmpty().WithMessage("OTP code is required.")
            .Length(4).WithMessage("OTP code must be exactly 4 digits.")
            .Matches("^[0-9]{4}$").WithMessage("OTP code must contain digits only.");
    }
}
