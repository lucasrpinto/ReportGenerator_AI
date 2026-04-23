namespace Relatorios.Application.Abstractions.Security;

public interface ISqlSafetyValidator
{
    void ValidateOrThrow(string sql);
}