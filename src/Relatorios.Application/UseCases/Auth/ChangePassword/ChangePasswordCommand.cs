namespace Relatorios.Application.UseCases.Auth.ChangePassword;

public sealed class ChangePasswordCommand
{
    public Guid UserId { get; set; }

    public string CurrentPassword { get; set; } = string.Empty;

    public string NewPassword { get; set; } = string.Empty;
}