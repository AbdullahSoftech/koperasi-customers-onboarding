using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Wrappers;

namespace Application.Commands.Customers;

public record SetupPinCommand(Guid CustomerId, string Pin) : IRequest<ApiResponse<bool>>;

public class SetupPinCommandHandler : IRequestHandler<SetupPinCommand, ApiResponse<bool>>
{
    private readonly IAppDbContext _context;

    public SetupPinCommandHandler(IAppDbContext context)
    {
        _context = context;
    }

    public async Task<ApiResponse<bool>> Handle(SetupPinCommand request, CancellationToken cancellationToken)
    {
        var customer = await _context.Customers
            .Include(c => c.Auth)
            .FirstOrDefaultAsync(c => c.Id == request.CustomerId, cancellationToken);

        if (customer is null)
            return ApiResponse<bool>.Fail("Customer not found.", "CUSTOMER_NOT_FOUND");

        var pinHash = BCrypt.Net.BCrypt.HashPassword(request.Pin);

        if (customer.Auth is null)
        {
            var auth = new CustomerAuth
            {
                CustomerId = customer.Id,
                PinHash = pinHash
            };
            _context.CustomerAuths.Add(auth);
        }
        else
        {
            customer.Auth.PinHash = pinHash;
            customer.Auth.UpdatedAt = DateTime.UtcNow;
        }

        // Activate customer
        customer.Status = CustomerStatus.Active;
        customer.UpdatedAt = DateTime.UtcNow;

        // Audit
        _context.AuditLogs.Add(new AuditLog
        {
            CustomerId = customer.Id,
            Action = "PIN_SETUP",
            Description = "Customer PIN configured successfully."
        });

        await _context.SaveChangesAsync(cancellationToken);

        return ApiResponse<bool>.Ok(true, "PIN set up successfully.");
    }
}