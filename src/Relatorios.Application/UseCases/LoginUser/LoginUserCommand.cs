namespace Relatorios.Application.UseCases.Auth.LoginUser;

public sealed class LoginUserCommand
{
    public string Email { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;
}