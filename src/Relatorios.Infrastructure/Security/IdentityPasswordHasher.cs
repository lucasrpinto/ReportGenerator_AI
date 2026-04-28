using Microsoft.AspNetCore.Identity;
using Relatorios.Application.Abstractions.Security;

namespace Relatorios.Infrastructure.Security;

public sealed class IdentityPasswordHasher : IPasswordHasher
{
    private static readonly object PasswordUser = new();

    private readonly PasswordHasher<object> _passwordHasher = new();

    public string Hash(string password)
    {
        return _passwordHasher.HashPassword(PasswordUser, password);
    }

    public bool Verify(string password, string passwordHash)
    {
        var result = _passwordHasher.VerifyHashedPassword(
            PasswordUser,
            passwordHash,
            password);

        return result is PasswordVerificationResult.Success
            or PasswordVerificationResult.SuccessRehashNeeded;
    }
}