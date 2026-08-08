using FluentResults;
using Microsoft.AspNetCore.Mvc;
using IdentityJwtAuthDemo.Application.Interfaces;
using IdentityJwtAuthDemo.Application.Auth.Requests;
using IdentityJwtAuthDemo.Application.Auth.Responses;

namespace IdentityJwtAuthDemo.PublicApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequestDto request)
    {
        var result = await _authService.RegisterAsync(request);
        if (result.IsFailed)
            return BadRequest(result.Errors.Select(e => e.Message));

        return Ok();
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequestDto request)
    {
        var result = await _authService.LoginAsync(request);
        if (result.IsFailed)
            return Unauthorized(result.Errors.Select(e => e.Message));

        return Ok(result.Value);
    }
}
