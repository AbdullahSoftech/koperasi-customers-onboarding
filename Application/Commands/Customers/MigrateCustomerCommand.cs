using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Wrappers;

namespace Application.Commands.Customers;

public record MigrateCustomerCommand(
    string PhoneNumber,
    Guid OtpRequestId,
    string? OldSystemRef
) : IRequest<ApiResponse<CustomerResponse>>;

public class MigrateCustomerCommandHandler
    : IRequestHandler<MigrateCustomerCommand, ApiResponse<CustomerResponse>>
{
    private readonly IAppDbContext _context;

    public MigrateCustomerCommandHandler(IAppDbContext context)
    {
        _context = context;
    }

    public async Task<ApiResponse<CustomerResponse>> Handle(
        MigrateCustomerCommand request, CancellationToken cancellationToken)
    {
        // Ensure OTP was verified for migration
        var otp = await _context.OtpRequests
            .FirstOrDefaultAsync(o => o.Id == request.OtpRequestId
                                   && o.PhoneNumber == request.PhoneNumber
                                   && o.IsVerified
                                   && o.Purpose == OtpPurpose.Migration,
                                 cancellationToken);

        if (otp is null)
            return ApiResponse<CustomerResponse>.Fail(
                "Phone number not verified. Please complete OTP verification first.",
                "OTP_NOT_VERIFIED");

        // Find existing customer
        var customer = await _context.Customers
            .FirstOrDefaultAsync(c => c.PhoneNumber == request.PhoneNumber, cancellationToken);

        if (customer is null)
            return ApiResponse<CustomerResponse>.Fail(
                "No existing account found with this phone number.",
                "ACCOUNT_NOT_FOUND");

        // Create migration record
        var migration = new MigrationRecord
        {
            CustomerId = customer.Id,
            OldSystemRef = request.OldSystemRef,
            MigrationStatus = "PENDING"
        };

        _context.MigrationRecords.Add(migration);

        // Update customer type
        customer.CustomerType = CustomerType.Migrated;
        customer.UpdatedAt = DateTime.UtcNow;

        otp.CustomerId = customer.Id;

        _context.AuditLogs.Add(new AuditLog
        {
            CustomerId = customer.Id,
            Action = "MIGRATION_STARTED",
            Description = $"Migration initiated for {request.PhoneNumber}"
        });

        await _context.SaveChangesAsync(cancellationToken);

        return ApiResponse<CustomerResponse>.Ok(
            MapToResponse(customer),
            "Migration initiated successfully.");
    }

    private static CustomerResponse MapToResponse(Customer c) => new(
        c.Id, 
        c.PhoneNumber, 
        c.Email,
        c.FullName,
        c.Status.ToString(), 
        c.CustomerType.ToString(),
        c.CreatedAt);
}