using FluentResults;
using IdentityJwtAuthDemo.Application.Auth.Requests;
using IdentityJwtAuthDemo.Application.Auth.Responses;

namespace IdentityJwtAuthDemo.Application.Interfaces;

public interface IAuthService
{
    Task<Result> RegisterAsync(RegisterRequestDto request);
    Task<Result<LoginResponseDto>> LoginAsync(LoginRequestDto request);
}
