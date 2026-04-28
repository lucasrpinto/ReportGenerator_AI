using Relatorios.Application.Abstractions.Persistence;
using Relatorios.Application.Abstractions.Security;
using Relatorios.Application.UseCases.Auth;
using Relatorios.Domain.Entities;

namespace Relatorios.Application.UseCases.Auth.RegisterUser;

public sealed class RegisterUserHandler
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;

    public RegisterUserHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator jwtTokenGenerator)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
    }

    public async Task<AuthResult> HandleAsync(
        RegisterUserCommand command,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.FullName))
            throw new ArgumentException("O nome é obrigatório.");

        if (string.IsNullOrWhiteSpace(command.Email))
            throw new ArgumentException("O e-mail é obrigatório.");

        if (string.IsNullOrWhiteSpace(command.Password) || command.Password.Length < 6)
            throw new ArgumentException("A senha deve ter no mínimo 6 caracteres.");

        var email = command.Email.Trim().ToLowerInvariant();

        var emailAlreadyExists = await _userRepository.ExistsByEmailAsync(email, cancellationToken);

        if (emailAlreadyExists)
            throw new InvalidOperationException("Já existe um usuário cadastrado com este e-mail.");

        var user = new User
        {
            Id = Guid.NewGuid(),
            FullName = command.FullName.Trim(),
            Email = email,
            PasswordHash = _passwordHasher.Hash(command.Password),
            Role = "User",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        await _userRepository.AddAsync(user, cancellationToken);

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