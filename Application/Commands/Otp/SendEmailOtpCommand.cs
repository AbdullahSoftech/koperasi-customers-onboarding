using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shared.Constants;
using Shared.Wrappers;

namespace Application.Commands.Otp;

public record SendEmailOtpCommand(Guid CustomerId, OtpPurpose Purpose, Guid? LoginSessionId = null)
    : IRequest<ApiResponse<OtpResponse>>;

public class SendEmailOtpCommandHandler : IRequestHandler<SendEmailOtpCommand, ApiResponse<OtpResponse>>
{
    private readonly IAppDbContext _context;
    private readonly ILogger<SendEmailOtpCommandHandler> _logger;

    public SendEmailOtpCommandHandler(IAppDbContext context, ILogger<SendEmailOtpCommandHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ApiResponse<OtpResponse>> Handle(
        SendEmailOtpCommand request, CancellationToken cancellationToken)
    {
        var customer = await _context.Customers
            .FirstOrDefaultAsync(c => c.Id == request.CustomerId, cancellationToken);

        if (customer is null)
            return ApiResponse<OtpResponse>.Fail("Customer not found.", "CUSTOMER_NOT_FOUND");

        if (request.Purpose == OtpPurpose.RegistrationEmail)
        {
            if (customer.Status != CustomerStatus.Pending)
                return ApiResponse<OtpResponse>.Fail(
                    "Account is not in a pending state.", "INVALID_STATUS");

            var mobileVerified = await _context.OtpRequests
                .AnyAsync(o => o.CustomerId == customer.Id
                            && o.Purpose == OtpPurpose.Registration
                            && o.IsVerified,
                          cancellationToken);

            if (!mobileVerified)
                return ApiResponse<OtpResponse>.Fail(
                    "Please verify your mobile OTP first.", "MOBILE_OTP_NOT_VERIFIED");
        }
        else if (request.Purpose == OtpPurpose.EmailVerification)
        {
            if (!request.LoginSessionId.HasValue)
                return ApiResponse<OtpResponse>.Fail(
                    "Login session ID is required.", "SESSION_REQUIRED");

            var session = await _context.LoginSessions
                .FirstOrDefaultAsync(s => s.Id == request.LoginSessionId.Value
                                       && s.CustomerId == customer.Id
                                       && !s.IsCompleted
                                       && s.ExpiresAt > DateTime.UtcNow,
                                     cancellationToken);

            if (session is null)
                return ApiResponse<OtpResponse>.Fail(
                    "Login session not found or expired.", "SESSION_INVALID");

            if (!session.IsPhoneOtpVerified)
                return ApiResponse<OtpResponse>.Fail(
                    "Please verify your phone OTP first.", "PHONE_OTP_NOT_VERIFIED");
        }
        else
        {
            return ApiResponse<OtpResponse>.Fail(
                "Invalid OTP purpose for email. Use RegistrationEmail or EmailVerification.",
                "INVALID_PURPOSE");
        }

        // Expire previous unverified OTPs for same email + purpose
        var previousOtps = await _context.OtpRequests
            .Where(o => o.EmailAddress == customer.Email
                     && o.Purpose == request.Purpose
                     && !o.IsVerified
                     && o.ExpiresAt > DateTime.UtcNow)
            .ToListAsync(cancellationToken);

        foreach (var old in previousOtps)
            old.ExpiresAt = DateTime.UtcNow;

        var otpCode = Random.Shared.Next(1000, 9999).ToString();

        var otpRequest = new OtpRequest
        {
            CustomerId = customer.Id,
            PhoneNumber = customer.PhoneNumber,
            EmailAddress = customer.Email,
            OtpCode = otpCode,
            Purpose = request.Purpose,
            ExpiresAt = DateTime.UtcNow.AddMinutes(AppConstants.OtpExpiryMinutes)
        };

        _context.OtpRequests.Add(otpRequest);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "[MOCK EMAIL] OTP for {Email} ({Purpose}): {OtpCode} — expires at {ExpiresAt}",
            customer.Email, request.Purpose, otpCode, otpRequest.ExpiresAt);

        return ApiResponse<OtpResponse>.Ok(
            new OtpResponse(otpRequest.Id, otpRequest.ExpiresAt),
            "OTP sent to your email address.");
    }
}
