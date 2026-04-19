using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Wrappers;

namespace Application.Commands.Auth;

public record InitiateLoginCommand(string IcNumber) : IRequest<ApiResponse<InitiateLoginResponse>>;

public class InitiateLoginCommandHandler
    : IRequestHandler<InitiateLoginCommand, ApiResponse<InitiateLoginResponse>>
{
    private readonly IAppDbContext _context;

    public InitiateLoginCommandHandler(IAppDbContext context)
    {
        _context = context;
    }

    public async Task<ApiResponse<InitiateLoginResponse>> Handle(
        InitiateLoginCommand request, CancellationToken cancellationToken)
    {
        // Find customer by IC number
        var customer = await _context.Customers
            .FirstOrDefaultAsync(c => c.NationalId == request.IcNumber, cancellationToken);

        if (customer is null)
            return ApiResponse<InitiateLoginResponse>.Fail(
                "You haven't registered yet. Please create an account first.",
                "NOT_REGISTERED");

        if (customer.Status == Domain.Enums.CustomerStatus.Pending)
            return ApiResponse<InitiateLoginResponse>.Fail(
                "Your registration is not complete. Please finish setting up your account.",
                "REGISTRATION_INCOMPLETE");

        if (customer.Status == Domain.Enums.CustomerStatus.Suspended)
            return ApiResponse<InitiateLoginResponse>.Fail(
                "Your account has been suspended. Please contact support.",
                "ACCOUNT_SUSPENDED");

        // Create a new login session (expires in 15 minutes)
        var session = new LoginSession
        {
            CustomerId = customer.Id,
            ExpiresAt = DateTime.UtcNow.AddMinutes(15)
        };

        _context.LoginSessions.Add(session);

        _context.AuditLogs.Add(new AuditLog
        {
            CustomerId = customer.Id,
            Action = "LOGIN_INITIATED",
            Description = $"Login initiated for IC: {request.IcNumber}"
        });

        await _context.SaveChangesAsync(cancellationToken);

        // Mask email — show only first 2 chars and domain
        var maskedEmail = MaskEmail(customer.Email);

        return ApiResponse<InitiateLoginResponse>.Ok(
            new InitiateLoginResponse(
                customer.Id,
                MaskPhone(customer.PhoneNumber),
                maskedEmail,
                session.Id),
            "Account found. Please verify your phone number.");
    }

    private static string MaskPhone(string phone)
    {
        if (phone.Length <= 4) return phone;
        return phone[..3] + new string('*', phone.Length - 6) + phone[^3..];
    }

    private static string MaskEmail(string email)
    {
        if (string.IsNullOrEmpty(email)) return string.Empty;
        var parts = email.Split('@');
        if (parts.Length != 2) return email;
        var name = parts[0];
        var visible = name.Length >= 2 ? name[..2] : name;
        return visible + new string('*', Math.Max(0, name.Length - 2)) + "@" + parts[1];
    }
}