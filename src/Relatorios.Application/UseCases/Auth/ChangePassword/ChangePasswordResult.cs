namespace Relatorios.Application.UseCases.Auth.ChangePassword;

public sealed class ChangePasswordResult
{
    public bool Success { get; set; }

    public string Message { get; set; } = string.Empty;

    public static ChangePasswordResult Ok()
    {
        return new ChangePasswordResult
        {
            Success = true,
            Message = "Senha alterada com sucesso."
        };
    }

    public static ChangePasswordResult Fail(string message)
    {
        return new ChangePasswordResult
        {
            Success = false,
            Message = message
        };
    }
}