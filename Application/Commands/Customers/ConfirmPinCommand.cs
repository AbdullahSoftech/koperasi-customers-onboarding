using Application.Interfaces;
using BC = BCrypt.Net.BCrypt;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Wrappers;

namespace Application.Commands.Customers;

public record ConfirmPinCommand(Guid CustomerId, string Pin) : IRequest<ApiResponse<bool>>;

public class ConfirmPinCommandHandler : IRequestHandler<ConfirmPinCommand, ApiResponse<bool>>
{
    private readonly IAppDbContext _context;

    public ConfirmPinCommandHandler(IAppDbContext context)
    {
        _context = context;
    }

    public async Task<ApiResponse<bool>> Handle(
        ConfirmPinCommand request, CancellationToken cancellationToken)
    {
        if (request.Pin.Length != 6 || !request.Pin.All(char.IsDigit))
            return ApiResponse<bool>.Fail("PIN must be exactly 6 digits.", "INVALID_PIN");

        var auth = await _context.CustomerAuths
            .FirstOrDefaultAsync(a => a.CustomerId == request.CustomerId, cancellationToken);

        if (auth is null)
            return ApiResponse<bool>.Fail(
                "No PIN found. Please set up your PIN first.",
                "PIN_NOT_SETUP");

        var isMatch = BC.Verify(request.Pin, auth.PinHash);

        if (!isMatch)
            return ApiResponse<bool>.Fail(
                "PIN does not match. Please try again.",
                "PIN_MISMATCH");

        return ApiResponse<bool>.Ok(true, "PIN confirmed successfully.");
    }
}