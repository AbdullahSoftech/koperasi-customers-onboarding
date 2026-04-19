using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shared.Constants;
using Shared.Wrappers;

namespace Application.Commands.Customers;

public record RegisterCustomerCommand(
    string FullName,
    string IcNumber,
    string MobileNumber,
    string EmailAddress
) : IRequest<ApiResponse<InitiateRegistrationResponse>>;

public class RegisterCustomerCommandHandler
    : IRequestHandler<RegisterCustomerCommand, ApiResponse<InitiateRegistrationResponse>>
{
    private readonly IAppDbContext _context;
    private readonly ILogger<RegisterCustomerCommandHandler> _logger;

    public RegisterCustomerCommandHandler(IAppDbContext context, ILogger<RegisterCustomerCommandHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ApiResponse<InitiateRegistrationResponse>> Handle(
        RegisterCustomerCommand request, CancellationToken cancellationToken)
    {
        var phoneExists = await _context.Customers
            .AnyAsync(c => c.PhoneNumber == request.MobileNumber, cancellationToken);

        if (phoneExists)
            return ApiResponse<InitiateRegistrationResponse>.Fail(
                "An account already exists with this mobile number.",
                "PHONE_ALREADY_REGISTERED");

        var icExists = await _context.Customers
            .AnyAsync(c => c.NationalId == request.IcNumber, cancellationToken);

        if (icExists)
            return ApiResponse<InitiateRegistrationResponse>.Fail(
                "An account already exists with this IC number.",
                "IC_ALREADY_REGISTERED");

        var emailExists = await _context.Customers
            .AnyAsync(c => c.Email == request.EmailAddress, cancellationToken);

        if (emailExists)
            return ApiResponse<InitiateRegistrationResponse>.Fail(
                "An account already exists with this email address.",
                "EMAIL_ALREADY_REGISTERED");

        var customer = new Customer
        {
            FullName = request.FullName,
            PhoneNumber = request.MobileNumber,
            Email = request.EmailAddress,
            NationalId = request.IcNumber,
            CustomerType = CustomerType.New,
            Status = CustomerStatus.Pending
        };

        _context.Customers.Add(customer);

        var otpCode = Random.Shared.Next(1000, 9999).ToString();

        var otpRequest = new OtpRequest
        {
            CustomerId = customer.Id,
            PhoneNumber = request.MobileNumber,
            OtpCode = otpCode,
            Purpose = OtpPurpose.Registration,
            ExpiresAt = DateTime.UtcNow.AddMinutes(AppConstants.OtpExpiryMinutes)
        };

        _context.OtpRequests.Add(otpRequest);

        _context.AuditLogs.Add(new AuditLog
        {
            CustomerId = customer.Id,
            Action = "REGISTRATION_INITIATED",
            Description = $"New account registration started for {request.MobileNumber}"
        });

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "[MOCK SMS] Registration OTP for {PhoneNumber}: {OtpCode} — expires at {ExpiresAt}",
            request.MobileNumber, otpCode, otpRequest.ExpiresAt);

        return ApiResponse<InitiateRegistrationResponse>.Ok(
            new InitiateRegistrationResponse(customer.Id, otpRequest.Id, otpRequest.ExpiresAt),
            "OTP sent to your mobile number. Please verify to continue.");
    }
}
