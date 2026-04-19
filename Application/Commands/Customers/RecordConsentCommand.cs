using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Wrappers;

namespace Application.Commands.Customers;

public record RecordConsentCommand(Guid CustomerId, string PolicyVersion, bool IsAccepted, string? IpAddress)
    : IRequest<ApiResponse<bool>>;

public class RecordConsentCommandHandler
    : IRequestHandler<RecordConsentCommand, ApiResponse<bool>>
{
    private readonly IAppDbContext _context;

    public RecordConsentCommandHandler(IAppDbContext context)
    {
        _context = context;
    }

    public async Task<ApiResponse<bool>> Handle(
        RecordConsentCommand request, CancellationToken cancellationToken)
    {
        var customer = await _context.Customers
            .FirstOrDefaultAsync(c => c.Id == request.CustomerId, cancellationToken);

        if (customer is null)
            return ApiResponse<bool>.Fail("Customer not found.", "CUSTOMER_NOT_FOUND");

        if (!request.IsAccepted)
            return ApiResponse<bool>.Fail(
                "Privacy policy must be accepted to proceed.",
                "POLICY_NOT_ACCEPTED");

        // For new customers, email OTP must be verified before proceeding
        if (customer.CustomerType == CustomerType.New)
        {
            var emailVerified = await _context.OtpRequests
                .AnyAsync(o => o.CustomerId == request.CustomerId
                            && o.Purpose == OtpPurpose.RegistrationEmail
                            && o.IsVerified,
                          cancellationToken);

            if (!emailVerified)
                return ApiResponse<bool>.Fail(
                    "Please verify your email OTP before accepting the privacy policy.",
                    "EMAIL_OTP_NOT_VERIFIED");
        }

        var consent = new PrivacyConsent
        {
            CustomerId = request.CustomerId,
            PolicyVersion = request.PolicyVersion,
            IsAccepted = request.IsAccepted,
            AcceptedAt = DateTime.UtcNow,
            IpAddress = request.IpAddress
        };

        _context.PrivacyConsents.Add(consent);

        _context.AuditLogs.Add(new AuditLog
        {
            CustomerId = request.CustomerId,
            Action = "CONSENT_RECORDED",
            Description = $"Privacy policy {request.PolicyVersion} accepted."
        });

        await _context.SaveChangesAsync(cancellationToken);

        return ApiResponse<bool>.Ok(true, "Consent recorded successfully.");
    }
}