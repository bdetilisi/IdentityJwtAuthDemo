using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using FluentResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using IdentityJwtAuthDemo.Application.Auth.Requests;
using IdentityJwtAuthDemo.Application.Auth.Responses;
using IdentityJwtAuthDemo.Application.Interfaces;

namespace IdentityJwtAuthDemo.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly UserManager<IdentityUser> _userManager;
    private readonly IConfiguration _configuration;

    public AuthService(
        UserManager<IdentityUser> userManager,
        IConfiguration configuration)
    {
        _userManager = userManager;
        _configuration = configuration;
    }

    public async Task<Result> RegisterAsync(RegisterRequestDto request)
    {
        var existing = await _userManager.FindByEmailAsync(request.Email);
        if (existing != null)
            return Result.Fail("Email already in use");

        var user = new IdentityUser { UserName = request.Email, Email = request.Email };
        var create = await _userManager.CreateAsync(user, request.Password);
        if (!create.Succeeded)
            return Result.Fail(string.Join(';', create.Errors.Select(e => e.Description)));

        // assign default 'User' role
        if (await _userManager.IsInRoleAsync(user, "User") == false)
        {
            await _userManager.AddToRoleAsync(user, "User");
        }

        return Result.Ok();
    }

    public async Task<Result<LoginResponseDto>> LoginAsync(LoginRequestDto request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null)
            return Result.Fail<LoginResponseDto>("Invalid credentials");

        var passwordValid = await _userManager.CheckPasswordAsync(user, request.Password);
        if (!passwordValid)
            return Result.Fail<LoginResponseDto>("Invalid credentials");

        var token = GenerateJwtToken(user);
        return Result.Ok(new LoginResponseDto(token.TokenString, token.ExpiresAt));
    }

    private (string TokenString, DateTime ExpiresAt) GenerateJwtToken(IdentityUser user)
    {
        var key = _configuration["Jwt:Key"] ?? string.Empty;
        var issuer = _configuration["Jwt:Issuer"] ?? string.Empty;
        var audience = _configuration["Jwt:Audience"] ?? string.Empty;
        var minutes = int.TryParse(_configuration["Jwt:ExpireMinutes"], out var m) ? m : 60;

        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id),
            new Claim(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.Name, user.UserName ?? string.Empty)
        };

        var roles = _userManager.GetRolesAsync(user).GetAwaiter().GetResult();
        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var expires = DateTime.UtcNow.AddMinutes(minutes);
        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        var creds = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: expires,
            signingCredentials: creds);

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);
        return (tokenString, expires);
    }
}
