namespace Relatorios.Application.UseCases.Auth.RegisterUser;

public sealed class RegisterUserCommand
{
    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;
}