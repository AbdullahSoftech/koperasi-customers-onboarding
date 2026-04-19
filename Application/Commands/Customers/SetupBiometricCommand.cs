using Application.Interfaces;
using Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Wrappers;

namespace Application.Commands.Customers;

public record SetupBiometricCommand(Guid CustomerId, string BiometricToken)
    : IRequest<ApiResponse<bool>>;

public class SetupBiometricCommandHandler
    : IRequestHandler<SetupBiometricCommand, ApiResponse<bool>>
{
    private readonly IAppDbContext _context;

    public SetupBiometricCommandHandler(IAppDbContext context)
    {
        _context = context;
    }

    public async Task<ApiResponse<bool>> Handle(
        SetupBiometricCommand request, CancellationToken cancellationToken)
    {
        var auth = await _context.CustomerAuths
            .FirstOrDefaultAsync(a => a.CustomerId == request.CustomerId, cancellationToken);

        if (auth is null)
            return ApiResponse<bool>.Fail(
                "Customer auth record not found. Please set up PIN first.",
                "AUTH_NOT_FOUND");

        auth.IsBiometricEnabled = true;
        auth.BiometricToken = request.BiometricToken;
        auth.UpdatedAt = DateTime.UtcNow;

        _context.AuditLogs.Add(new AuditLog
        {
            CustomerId = request.CustomerId,
            Action = "BIOMETRIC_ENABLED",
            Description = "Biometric authentication enabled."
        });

        await _context.SaveChangesAsync(cancellationToken);

        return ApiResponse<bool>.Ok(true, "Biometric authentication enabled successfully.");
    }
}