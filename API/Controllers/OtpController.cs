using Application.Commands.Otp;
using Application.DTOs;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Shared.Wrappers;

namespace API.Controllers;

[ApiController]
[Route("api/otp")]
public class OtpController : ControllerBase
{
    private readonly IMediator _mediator;

    public OtpController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Send OTP to phone number. Purpose: Login or Migration.
    /// (Registration OTP is issued automatically via POST /api/customers/register)
    /// </summary>
    [HttpPost("send")]
    [ProducesResponseType(typeof(ApiResponse<OtpResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse<OtpResponse>), 400)]
    public async Task<IActionResult> SendOtp([FromBody] SendOtpRequest request)
    {
        var result = await _mediator.Send(
            new SendOtpCommand(request.PhoneNumber, request.Purpose));

        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Verify phone OTP. Pass loginSessionId for login flow, omit for migration/registration.
    /// </summary>
    [HttpPost("verify")]
    [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
    [ProducesResponseType(typeof(ApiResponse<bool>), 400)]
    public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpRequest request)
    {
        var result = await _mediator.Send(
            new VerifyOtpCommand(
                request.OtpRequestId,
                request.PhoneNumber,
                request.OtpCode,
                request?.LoginSessionId));

        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Send OTP to email address.
    /// Purpose: RegistrationEmail (registration flow) or EmailVerification (login flow).
    /// Pass loginSessionId when Purpose = EmailVerification.
    /// </summary>
    [HttpPost("email/send")]
    [ProducesResponseType(typeof(ApiResponse<OtpResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse<OtpResponse>), 400)]
    public async Task<IActionResult> SendEmailOtp([FromBody] SendEmailOtpRequest request)
    {
        var result = await _mediator.Send(
            new SendEmailOtpCommand(request.CustomerId, request.Purpose, request?.LoginSessionId));

        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Verify email OTP.
    /// Pass loginSessionId when verifying for login flow (EmailVerification).
    /// </summary>
    [HttpPost("email/verify")]
    [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
    [ProducesResponseType(typeof(ApiResponse<bool>), 400)]
    public async Task<IActionResult> VerifyEmailOtp([FromBody] VerifyEmailOtpRequest request)
    {
        var result = await _mediator.Send(
            new VerifyEmailOtpCommand(
                request.CustomerId,
                request.OtpRequestId,
                request.OtpCode,
                request.LoginSessionId));

        return result.Success ? Ok(result) : BadRequest(result);
    }
}
