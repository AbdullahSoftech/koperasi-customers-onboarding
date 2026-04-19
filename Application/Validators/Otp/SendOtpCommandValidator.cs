using Application.Commands.Otp;
using FluentValidation;

namespace Application.Validators.Otp;

public class SendOtpCommandValidator : AbstractValidator<SendOtpCommand>
{
    public SendOtpCommandValidator()
    {
        RuleFor(x => x.PhoneNumber)
            .NotEmpty().WithMessage("Phone number is required.")
            .Matches(@"^\+?[0-9]{10,15}$").WithMessage("Phone number must be 10–15 digits.");

        RuleFor(x => x.Purpose)
            .IsInEnum().WithMessage("Invalid OTP purpose.");
    }
}
