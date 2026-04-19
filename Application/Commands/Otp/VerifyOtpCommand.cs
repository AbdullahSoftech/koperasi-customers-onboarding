using Application.Interfaces;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Constants;
using Shared.Wrappers;

namespace Application.Commands.Otp;

public record VerifyOtpCommand(
    Guid OtpRequestId,
    string PhoneNumber,
    string OtpCode,
    Guid? LoginSessionId = null
) : IRequest<ApiResponse<bool>>;

public class VerifyOtpCommandHandler : IRequestHandler<VerifyOtpCommand, ApiResponse<bool>>
{
    private readonly IAppDbContext _context;

    public VerifyOtpCommandHandler(IAppDbContext context)
    {
        _context = context;
    }

    public async Task<ApiResponse<bool>> Handle(VerifyOtpCommand request, CancellationToken cancellationToken)
    {
        var otpRequest = await _context.OtpRequests
            .FirstOrDefaultAsync(o => o.Id == request.OtpRequestId
                                   && o.PhoneNumber == request.PhoneNumber,
                                 cancellationToken);

        if (otpRequest is null)
            return ApiResponse<bool>.Fail("OTP request not found.", "OTP_NOT_FOUND");

        if (otpRequest.IsVerified)
            return ApiResponse<bool>.Fail("OTP has already been used.", "OTP_ALREADY_USED");

        if (otpRequest.ExpiresAt < DateTime.UtcNow)
            return ApiResponse<bool>.Fail("OTP has expired. Please request a new one.", "OTP_EXPIRED");

        if (otpRequest.AttemptCount >= AppConstants.OtpMaxAttempts)
            return ApiResponse<bool>.Fail(
                "Maximum attempts exceeded. Please request a new OTP.",
                "OTP_MAX_ATTEMPTS");

        // Increment attempt count regardless of result
        otpRequest.AttemptCount++;

        if (otpRequest.OtpCode != request.OtpCode)
        {
            await _context.SaveChangesAsync(cancellationToken);
            var remaining = AppConstants.OtpMaxAttempts - otpRequest.AttemptCount;
            return ApiResponse<bool>.Fail(
                $"Invalid OTP. {remaining} attempt(s) remaining.",
                "OTP_INVALID");
        }

        // Mark as verified
        otpRequest.IsVerified = true;

        // If this is a Login OTP, mark phone verified on the login session
        if (otpRequest.Purpose == OtpPurpose.Login && request.LoginSessionId.HasValue)
        {
            var session = await _context.LoginSessions
                .FirstOrDefaultAsync(s => s.Id == request.LoginSessionId.Value
                                       && !s.IsCompleted
                                       && s.ExpiresAt > DateTime.UtcNow,
                                     cancellationToken);

            if (session is not null)
                session.IsPhoneOtpVerified = true;
        }

        await _context.SaveChangesAsync(cancellationToken);

        return ApiResponse<bool>.Ok(true, "OTP verified successfully.");
    }
}