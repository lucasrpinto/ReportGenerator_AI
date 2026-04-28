namespace Relatorios.Application.Abstractions.Security;

public sealed class JwtTokenResult
{
    public string AccessToken { get; set; } = string.Empty;

    public DateTime ExpiresAt { get; set; }
}