using Application.Commands.Customers;
using Application.DTOs;
using Application.Queries.Customers;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Shared.Wrappers;

namespace API.Controllers;

[ApiController]
[Route("api/customers")]
public class CustomersController : ControllerBase
{
    private readonly IMediator _mediator;

    public CustomersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Step 1 — Initiate registration: provide details, receive mobile OTP.
    /// </summary>
    [HttpPost("register")]
    [ProducesResponseType(typeof(ApiResponse<InitiateRegistrationResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse<InitiateRegistrationResponse>), 400)]
    public async Task<IActionResult> Register([FromBody] RegisterCustomerRequest request)
    {
        var result = await _mediator.Send(new RegisterCustomerCommand(
            request.FullName,
            request.IcNumber,
            request.MobileNumber,
            request.EmailAddress));

        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Step 5 — Accept the privacy policy.
    /// </summary>
    [HttpPost("{id:guid}/consent")]
    [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
    [ProducesResponseType(typeof(ApiResponse<bool>), 400)]
    public async Task<IActionResult> RecordConsent(Guid id, [FromBody] ConsentRequest request)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await _mediator.Send(
            new RecordConsentCommand(id, request.PolicyVersion, request.IsAccepted, ipAddress));

        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Step 6 — Create 6-digit PIN.
    /// </summary>
    [HttpPost("{id:guid}/auth/setup-pin")]
    [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
    [ProducesResponseType(typeof(ApiResponse<bool>), 400)]
    public async Task<IActionResult> SetupPin(Guid id, [FromBody] SetupPinRequest request)
    {
        var result = await _mediator.Send(new SetupPinCommand(id, request.Pin));
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Step 7 — Confirm PIN by re-entering it.
    /// </summary>
    [HttpPost("{id:guid}/auth/confirm-pin")]
    [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
    [ProducesResponseType(typeof(ApiResponse<bool>), 400)]
    public async Task<IActionResult> ConfirmPin(Guid id, [FromBody] SetupPinRequest request)
    {
        var result = await _mediator.Send(new ConfirmPinCommand(id, request.Pin));
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Step 8 — Enable biometric authentication (optional).
    /// </summary>
    [HttpPut("{id:guid}/auth/biometric")]
    [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
    [ProducesResponseType(typeof(ApiResponse<bool>), 400)]
    public async Task<IActionResult> SetupBiometric(Guid id, [FromBody] BiometricRequest request)
    {
        var result = await _mediator.Send(new SetupBiometricCommand(id, request.BiometricToken));
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Migrate an existing customer to the new platform (after OTP verified).
    /// </summary>
    [HttpPost("migrate")]
    [ProducesResponseType(typeof(ApiResponse<MigrateCustomerResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse<MigrateCustomerResponse>), 400)]
    public async Task<IActionResult> Migrate([FromBody] MigrateCustomerRequest request)
    {
        var result = await _mediator.Send(new MigrateCustomerCommand(
            request.FullName,
            request.PhoneNumber,
            request.Email,
            request.NationalId,
            request.CustomerType,
            request.OldSystemRef,
            request.Notes)
        );

        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Get customer profile by ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<CustomerResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse<CustomerResponse>), 404)]
    public async Task<IActionResult> GetCustomer(Guid id)
    {
        var result = await _mediator.Send(new GetCustomerQuery(id));
        return result.Success ? Ok(result) : NotFound(result);
    }
}
