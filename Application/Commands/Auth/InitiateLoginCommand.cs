using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shared.Constants;
using Shared.Wrappers;

namespace Application.Commands.Auth;

public record InitiateLoginCommand(string IcNumber) : IRequest<ApiResponse<InitiateLoginResponse>>;

public class InitiateLoginCommandHandler
    : IRequestHandler<InitiateLoginCommand, ApiResponse<InitiateLoginResponse>>
{
    private readonly IAppDbContext _context;
    private readonly ILogger<InitiateLoginCommandHandler> _logger;

    public InitiateLoginCommandHandler(
        IAppDbContext context,
        ILogger<InitiateLoginCommandHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ApiResponse<InitiateLoginResponse>> Handle(
        InitiateLoginCommand request, CancellationToken cancellationToken)
    {
        // Include Migration
        var customer = await _context.Customers
            .Include(c => c.Migration)
            .FirstOrDefaultAsync(c => c.NationalId == request.IcNumber, cancellationToken);

        // Customer not found 
        if (customer is null)
            return ApiResponse<InitiateLoginResponse>.Fail(
                "You haven't registered yet. Please create an account first.",
                "NOT_REGISTERED");

        // Suspended
        if (customer.Status == CustomerStatus.Suspended)
            return ApiResponse<InitiateLoginResponse>.Fail(
                "Your account has been suspended. Please contact support.",
                "ACCOUNT_SUSPENDED");

        // Migration Pending
        if (customer.Migration?.MigrationStatus == "PENDING")
            return await SendOtpAsync(
                customer,
                purpose: OtpPurpose.Migration,
                auditAction: "MIGRATION_LOGIN_INITIATED",
                auditDescription: $"Migrated customer login initiated for IC: {request.IcNumber}",
                responseMessage: "Welcome! Please verify your phone number to activate your account.",
                cancellationToken);

        // Pending
        if (customer.Status == CustomerStatus.Pending)
            return await SendOtpAsync(
                customer,
                purpose: OtpPurpose.Registration,
                auditAction: "REGISTRATION_RESUMED",
                auditDescription: $"Pending customer resumed registration for IC: {request.IcNumber}",
                responseMessage: "Please complete your account verification.",
                cancellationToken);

        return await SendOtpAsync(
            customer,
            purpose: OtpPurpose.Login,
            auditAction: "LOGIN_INITIATED",
            auditDescription: $"Login initiated for IC: {request.IcNumber}",
            responseMessage: "OTP sent to your registered phone number.",
            cancellationToken);
    }

    /// <summary>
    /// Creates OTP, LoginSession, AuditLog — then returns masked response.
    /// </summary>
    private async Task<ApiResponse<InitiateLoginResponse>> SendOtpAsync(
        Customer customer,
        OtpPurpose purpose,
        string auditAction,
        string auditDescription,
        string responseMessage,
        CancellationToken cancellationToken)
    {
        var otpCode = Random.Shared.Next(1000, 9999).ToString();

        var otpRequest = new OtpRequest
        {
            CustomerId = customer.Id,
            PhoneNumber = customer.PhoneNumber,
            OtpCode = otpCode,
            Purpose = purpose,
            ExpiresAt = DateTime.UtcNow.AddMinutes(AppConstants.OtpExpiryMinutes)
        };

        var session = new LoginSession
        {
            CustomerId = customer.Id,
            ExpiresAt = DateTime.UtcNow.AddMinutes(15)
        };

        _context.OtpRequests.Add(otpRequest);
        _context.LoginSessions.Add(session);
        _context.AuditLogs.Add(new AuditLog
        {
            CustomerId = customer.Id,
            Action = auditAction,
            Description = auditDescription
        });

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "[MOCK SMS] OTP for {PhoneNumber}: {OtpCode} — expires at {ExpiresAt}",
            customer.PhoneNumber, otpCode, otpRequest.ExpiresAt);

        return ApiResponse<InitiateLoginResponse>.Ok(
            new InitiateLoginResponse(
                customer.Id,
                otpRequest.Id,
                customer.PhoneNumber,
                customer.Email,
                session.Id),
            responseMessage);
    }
}