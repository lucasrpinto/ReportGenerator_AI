using Relatorios.Application.Abstractions.Persistence;
using Relatorios.Application.Abstractions.Security;
using Relatorios.Application.UseCases.Auth;

namespace Relatorios.Application.UseCases.Auth.LoginUser;

public sealed class LoginUserHandler
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;

    public LoginUserHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator jwtTokenGenerator)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
    }

    public async Task<AuthResult?> HandleAsync(
        LoginUserCommand command,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.Email))
            throw new ArgumentException("O e-mail é obrigatório.");

        if (string.IsNullOrWhiteSpace(command.Password))
            throw new ArgumentException("A senha é obrigatória.");

        var email = command.Email.Trim().ToLowerInvariant();

        var user = await _userRepository.GetByEmailAsync(email, cancellationToken);

        if (user is null || !user.IsActive)
            return null;

        var passwordIsValid = _passwordHasher.Verify(command.Password, user.PasswordHash);

        if (!passwordIsValid)
            return null;

        await _userRepository.UpdateLastLoginAsync(user.Id, cancellationToken);

        var token = _jwtTokenGenerator.Generate(user);

        return new AuthResult
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            Role = user.Role,
            AccessToken = token.AccessToken,
            ExpiresAt = token.ExpiresAt
        };
    }
}