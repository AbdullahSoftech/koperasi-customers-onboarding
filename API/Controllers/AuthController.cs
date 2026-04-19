using Application.Commands.Auth;
using Application.DTOs;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Shared.Wrappers;

namespace API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;

    public AuthController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Step 1 — Enter IC number to initiate login.
    /// Returns masked phone and email, plus loginSessionId.
    /// </summary>
    [HttpPost("login/initiate")]
    [ProducesResponseType(typeof(ApiResponse<InitiateLoginResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse<InitiateLoginResponse>), 400)]
    public async Task<IActionResult> InitiateLogin([FromBody] InitiateLoginRequest request)
    {
        var result = await _mediator.Send(new InitiateLoginCommand(request.IcNumber));
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Step 5 — Enter 6-digit PIN to complete login (after both OTPs verified).
    /// </summary>
    [HttpPost("login/complete")]
    [ProducesResponseType(typeof(ApiResponse<LoginResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse<LoginResponse>), 400)]
    public async Task<IActionResult> CompleteLogin([FromBody] CompleteLoginRequest request)
    {
        var result = await _mediator.Send(
            new CompleteLoginCommand(request.LoginSessionId, request.CustomerId, request.Pin));
        return result.Success ? Ok(result) : BadRequest(result);
    }
}
