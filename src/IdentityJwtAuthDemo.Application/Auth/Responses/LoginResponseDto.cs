namespace IdentityJwtAuthDemo.Application.Auth.Responses;

public record LoginResponseDto(string Token, DateTime ExpiresAt);
