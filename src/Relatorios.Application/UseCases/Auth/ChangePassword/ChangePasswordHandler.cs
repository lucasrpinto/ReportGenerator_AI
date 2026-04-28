using Relatorios.Application.Abstractions.Persistence;
using Relatorios.Application.Abstractions.Security;

namespace Relatorios.Application.UseCases.Auth.ChangePassword;

public sealed class ChangePasswordHandler
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;

    public ChangePasswordHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
    }

    public async Task<ChangePasswordResult> HandleAsync(
        ChangePasswordCommand command,
        CancellationToken cancellationToken)
    {
        if (command.UserId == Guid.Empty)
        {
            return ChangePasswordResult.Fail("Usuário inválido.");
        }

        if (string.IsNullOrWhiteSpace(command.CurrentPassword))
        {
            return ChangePasswordResult.Fail("Informe a senha atual.");
        }

        if (string.IsNullOrWhiteSpace(command.NewPassword))
        {
            return ChangePasswordResult.Fail("Informe a nova senha.");
        }

        if (command.NewPassword.Length < 6)
        {
            return ChangePasswordResult.Fail("A nova senha deve ter no mínimo 6 caracteres.");
        }

        if (command.CurrentPassword == command.NewPassword)
        {
            return ChangePasswordResult.Fail("A nova senha deve ser diferente da senha atual.");
        }

        var user = await _userRepository.GetByIdAsync(
            command.UserId,
            cancellationToken);

        if (user is null || !user.IsActive)
        {
            return ChangePasswordResult.Fail("Usuário não encontrado ou inativo.");
        }

        var currentPasswordIsValid = _passwordHasher.Verify(
            command.CurrentPassword,
            user.PasswordHash);

        if (!currentPasswordIsValid)
        {
            return ChangePasswordResult.Fail("Senha atual inválida.");
        }

        var newPasswordHash = _passwordHasher.Hash(command.NewPassword);

        await _userRepository.UpdatePasswordAsync(
            user.Id,
            newPasswordHash,
            cancellationToken);

        return ChangePasswordResult.Ok();
    }
}