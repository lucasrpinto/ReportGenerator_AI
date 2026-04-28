using Relatorios.Domain.Entities;

namespace Relatorios.Application.Abstractions.Security;

public interface IJwtTokenGenerator
{
    JwtTokenResult Generate(User user);
}