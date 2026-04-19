using Application.DTOs;
using Application.Interfaces;
using BC = BCrypt.Net.BCrypt;
using Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Wrappers;

namespace Application.Commands.Auth;

public record CompleteLoginCommand(Guid LoginSessionId, Guid CustomerId, string Pin)
    : IRequest<ApiResponse<LoginResponse>>;

public class CompleteLoginCommandHandler
    : IRequestHandler<CompleteLoginCommand, ApiResponse<LoginResponse>>
{
    private readonly IAppDbContext _context;

    public CompleteLoginCommandHandler(IAppDbContext context)
    {
        _context = context;
    }

    public async Task<ApiResponse<LoginResponse>> Handle(
        CompleteLoginCommand request, CancellationToken cancellationToken)
    {
        // Validate session
        var session = await _context.LoginSessions
            .FirstOrDefaultAsync(s => s.Id == request.LoginSessionId
                                   && s.CustomerId == request.CustomerId
                                   && !s.IsCompleted
                                   && s.ExpiresAt > DateTime.UtcNow,
                                 cancellationToken);

        if (session is null)
            return ApiResponse<LoginResponse>.Fail(
                "Login session not found or expired. Please start again.",
                "SESSION_INVALID");

        // Both OTPs must be verified
        if (!session.IsPhoneOtpVerified)
            return ApiResponse<LoginResponse>.Fail(
                "Phone OTP not verified.",
                "PHONE_OTP_NOT_VERIFIED");

        if (!session.IsEmailOtpVerified)
            return ApiResponse<LoginResponse>.Fail(
                "Email OTP not verified.",
                "EMAIL_OTP_NOT_VERIFIED");

        // Get customer
        var customer = await _context.Customers
            .Include(c => c.Auth)
            .FirstOrDefaultAsync(c => c.Id == request.CustomerId, cancellationToken);

        if (customer is null)
            return ApiResponse<LoginResponse>.Fail("Customer not found.", "CUSTOMER_NOT_FOUND");

        // Validate PIN
        if (customer.Auth is null)
            return ApiResponse<LoginResponse>.Fail(
                "No PIN found for this account. Please contact support.",
                "PIN_NOT_SETUP");

        if (!BC.Verify(request.Pin, customer.Auth.PinHash))
            return ApiResponse<LoginResponse>.Fail(
                "Invalid PIN. Please try again.",
                "PIN_INVALID");

        // Mark session complete
        session.IsCompleted = true;
        customer.Auth.LastLoginAt = DateTime.UtcNow;

        _context.AuditLogs.Add(new AuditLog
        {
            CustomerId = customer.Id,
            Action = "LOGIN_COMPLETED",
            Description = "Customer logged in successfully."
        });

        await _context.SaveChangesAsync(cancellationToken);

        return ApiResponse<LoginResponse>.Ok(
            new LoginResponse(
                customer.Id,
                customer.FullName,
                customer.PhoneNumber,
                customer.Status.ToString(),
                DateTime.UtcNow),
            "Login successful. Welcome back!");
    }
}