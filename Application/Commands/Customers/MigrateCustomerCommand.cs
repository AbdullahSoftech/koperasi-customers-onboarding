using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Wrappers;

namespace Application.Commands.Customers;

public record MigrateCustomerCommand(
    string FullName,
    string PhoneNumber,
    string Email,
    string NationalId,
    CustomerType CustomerType,
    string? OldSystemRef,
    string? Notes
) : IRequest<ApiResponse<MigrateCustomerResponse>>;

public class MigrateCustomerCommandHandler
    : IRequestHandler<MigrateCustomerCommand, ApiResponse<MigrateCustomerResponse>>
{
    private readonly IAppDbContext _context;

    public MigrateCustomerCommandHandler(IAppDbContext context)
    {
        _context = context;
    }

    public async Task<ApiResponse<MigrateCustomerResponse>> Handle(
        MigrateCustomerCommand request, CancellationToken cancellationToken)
    {
        // Prevent duplicate migration
        var alreadyExists = await _context.Customers
            .AnyAsync(c => c.NationalId == request.NationalId, cancellationToken);

        if (alreadyExists)
            return ApiResponse<MigrateCustomerResponse>.Fail(
                "A customer with this IC number already exists.",
                "ALREADY_EXISTS");

        // Insert customer with Pending status
        var customer = new Customer
        {
            FullName = request.FullName,
            PhoneNumber = request.PhoneNumber,
            Email = request.Email,
            NationalId = request.NationalId,
            CustomerType = request.CustomerType,
            Status = CustomerStatus.Pending
        };

        // Migration record
        var migration = new MigrationRecord
        {
            CustomerId = customer.Id,
            OldSystemRef = request.OldSystemRef,
            MigrationStatus = "PENDING",     // <-- flipped to COMPLETED after they verify OTP
            MigratedAt = null,               // <-- set when they successfully verify
            Notes = request.Notes ?? "Migrated from legacy system"
        };

        _context.Customers.Add(customer);
        _context.MigrationRecords.Add(migration);
        _context.AuditLogs.Add(new AuditLog
        {
            CustomerId = customer.Id,
            Action = "CUSTOMER_MIGRATED",
            Description = $"Customer migrated from old system. OldSystemRef: {request.OldSystemRef}"
        });

        await _context.SaveChangesAsync(cancellationToken);

        return ApiResponse<MigrateCustomerResponse>.Ok(
            new MigrateCustomerResponse(
                customer.Id,
                customer.FullName,
                customer.PhoneNumber,
                customer.NationalId,
                customer.Status.ToString()),
            "Customer migrated successfully. They will be prompted to verify on first login.");
    }
}