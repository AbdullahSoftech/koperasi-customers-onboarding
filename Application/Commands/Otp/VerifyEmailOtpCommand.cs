using Application.Interfaces;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Constants;
using Shared.Wrappers;

namespace Application.Commands.Otp;

public record VerifyEmailOtpCommand(
    Guid CustomerId,
    Guid OtpRequestId,
    string OtpCode,
    Guid? LoginSessionId = null
) : IRequest<ApiResponse<bool>>;

public class VerifyEmailOtpCommandHandler : IRequestHandler<VerifyEmailOtpCommand, ApiResponse<bool>>
{
    private readonly IAppDbContext _context;

    public VerifyEmailOtpCommandHandler(IAppDbContext context)
    {
        _context = context;
    }

    public async Task<ApiResponse<bool>> Handle(
        VerifyEmailOtpCommand request, CancellationToken cancellationToken)
    {
        var customer = await _context.Customers
            .FirstOrDefaultAsync(c => c.Id == request.CustomerId, cancellationToken);

        if (customer is null)
            return ApiResponse<bool>.Fail("Customer not found.", "CUSTOMER_NOT_FOUND");

        var otpRequest = await _context.OtpRequests
            .FirstOrDefaultAsync(o => o.Id == request.OtpRequestId
                                   && o.CustomerId == request.CustomerId
                                   && (o.Purpose == OtpPurpose.RegistrationEmail
                                    || o.Purpose == OtpPurpose.EmailVerification),
                                 cancellationToken);

        if (otpRequest is null)
            return ApiResponse<bool>.Fail("OTP request not found.", "OTP_NOT_FOUND");

        if (otpRequest.IsVerified)
            return ApiResponse<bool>.Fail("OTP has already been used.", "OTP_ALREADY_USED");

        if (otpRequest.ExpiresAt < DateTime.UtcNow)
            return ApiResponse<bool>.Fail("OTP has expired. Please request a new one.", "OTP_EXPIRED");

        if (otpRequest.AttemptCount >= AppConstants.OtpMaxAttempts)
            return ApiResponse<bool>.Fail(
                "Maximum attempts exceeded. Please request a new OTP.", "OTP_MAX_ATTEMPTS");

        otpRequest.AttemptCount++;

        if (otpRequest.OtpCode != request.OtpCode)
        {
            await _context.SaveChangesAsync(cancellationToken);
            var remaining = AppConstants.OtpMaxAttempts - otpRequest.AttemptCount;
            return ApiResponse<bool>.Fail(
                $"Invalid OTP. {remaining} attempt(s) remaining.", "OTP_INVALID");
        }

        otpRequest.IsVerified = true;

        // Login flow: mark email OTP verified on the session
        if (otpRequest.Purpose == OtpPurpose.EmailVerification && request.LoginSessionId.HasValue)
        {
            var session = await _context.LoginSessions
                .FirstOrDefaultAsync(s => s.Id == request.LoginSessionId.Value
                                       && !s.IsCompleted
                                       && s.ExpiresAt > DateTime.UtcNow,
                                     cancellationToken);

            if (session is not null)
                session.IsEmailOtpVerified = true;
        }

        await _context.SaveChangesAsync(cancellationToken);

        return ApiResponse<bool>.Ok(true, "Email OTP verified successfully.");
    }
}
