using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Relatorios.Application.UseCases.Auth.ChangePassword;
using Relatorios.Application.UseCases.Auth.LoginUser;
using Relatorios.Application.UseCases.Auth.RegisterUser;
using Relatorios.Contracts.Requests;
using Relatorios.Contracts.Responses;
using System.Security.Claims;

namespace Relatorios.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Register(
        [FromBody] RegisterUserRequest request,
        [FromServices] RegisterUserHandler handler,
        CancellationToken cancellationToken)
    {
        var command = new RegisterUserCommand
        {
            FullName = request.FullName,
            Email = request.Email,
            Password = request.Password
        };

        var result = await handler.HandleAsync(command, cancellationToken);

        return Ok(new AuthResponse
        {
            Id = result.Id,
            FullName = result.FullName,
            Email = result.Email,
            Role = result.Role,
            AccessToken = result.AccessToken,
            ExpiresAt = result.ExpiresAt
        });
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(
        [FromBody] LoginUserRequest request,
        [FromServices] LoginUserHandler handler,
        CancellationToken cancellationToken)
    {
        var command = new LoginUserCommand
        {
            Email = request.Email,
            Password = request.Password
        };

        var result = await handler.HandleAsync(command, cancellationToken);

        if (result is null)
        {
            return Unauthorized(new
            {
                success = false,
                message = "E-mail ou senha inválidos."
            });
        }

        return Ok(new AuthResponse
        {
            Id = result.Id,
            FullName = result.FullName,
            Email = result.Email,
            Role = result.Role,
            AccessToken = result.AccessToken,
            ExpiresAt = result.ExpiresAt
        });
    }

    [HttpPut("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword(
    [FromBody] ChangePasswordRequest request,
    [FromServices] ChangePasswordHandler handler,
    CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new
            {
                success = false,
                message = "Token inválido."
            });
        }

        var command = new ChangePasswordCommand
        {
            UserId = userId,
            CurrentPassword = request.CurrentPassword,
            NewPassword = request.NewPassword
        };

        var result = await handler.HandleAsync(command, cancellationToken);

        if (!result.Success)
        {
            return BadRequest(new
            {
                success = false,
                message = result.Message
            });
        }

        return Ok(new
        {
            success = true,
            message = result.Message
        });
    }
}