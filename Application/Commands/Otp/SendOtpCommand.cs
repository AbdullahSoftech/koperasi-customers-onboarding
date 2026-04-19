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

public record SendOtpCommand(string PhoneNumber, OtpPurpose Purpose)
    : IRequest<ApiResponse<OtpResponse>>;

public class SendOtpCommandHandler : IRequestHandler<SendOtpCommand, ApiResponse<OtpResponse>>
{
    private readonly IAppDbContext _context;
    private readonly ILogger<SendOtpCommandHandler> _logger;

    public SendOtpCommandHandler(IAppDbContext context, ILogger<SendOtpCommandHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ApiResponse<OtpResponse>> Handle(SendOtpCommand request, CancellationToken cancellationToken)
    {
        // Registration OTP is now issued automatically via POST /api/customers/register
        if (request.Purpose == OtpPurpose.Registration)
            return ApiResponse<OtpResponse>.Fail(
                "To register, use POST /api/customers/register instead.",
                "USE_REGISTRATION_ENDPOINT");

        // For MIGRATION: phone MUST exist in the system
        if (request.Purpose == OtpPurpose.Migration)
        {
            var exists = await _context.Customers
                .AnyAsync(c => c.PhoneNumber == request.PhoneNumber, cancellationToken);

            if (!exists)
                return ApiResponse<OtpResponse>.Fail(
                    "No existing account found with this phone number.",
                    "ACCOUNT_NOT_FOUND");
        }

        // Invalidate any previous unused OTPs for same phone + purpose
        var previousOtps = await _context.OtpRequests
            .Where(o => o.PhoneNumber == request.PhoneNumber
                     && o.Purpose == request.Purpose
                     && !o.IsVerified
                     && o.ExpiresAt > DateTime.UtcNow)
            .ToListAsync(cancellationToken);

        foreach (var old in previousOtps)
            old.ExpiresAt = DateTime.UtcNow; // expire them immediately

        // Generate new 4-digit OTP
        var otpCode = Random.Shared.Next(1000, 9999).ToString();

        var otpRequest = new OtpRequest
        {
            PhoneNumber = request.PhoneNumber,
            OtpCode = otpCode,
            Purpose = request.Purpose,
            ExpiresAt = DateTime.UtcNow.AddMinutes(AppConstants.OtpExpiryMinutes)
        };

        _context.OtpRequests.Add(otpRequest);
        await _context.SaveChangesAsync(cancellationToken);

        // Mock SMS — log OTP to console (replace with real SMS gateway later)
        _logger.LogInformation(
            "[MOCK SMS] OTP for {PhoneNumber} ({Purpose}): {OtpCode} — expires at {ExpiresAt}",
            request.PhoneNumber, request.Purpose, otpCode, otpRequest.ExpiresAt);

        return ApiResponse<OtpResponse>.Ok(
            new OtpResponse(otpRequest.Id, otpRequest.ExpiresAt),
            "OTP sent successfully.");
    }
}