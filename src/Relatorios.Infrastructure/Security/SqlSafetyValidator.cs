using System.Text.RegularExpressions;
using Relatorios.Application.Abstractions.Security;

namespace Relatorios.Infrastructure.Security;

public sealed class SqlSafetyValidator : ISqlSafetyValidator
{
    private static readonly string[] ForbiddenKeywords =
    [
        "INSERT",
        "UPDATE",
        "DELETE",
        "DROP",
        "CREATE",
        "ALTER",
        "TRUNCATE",
        "MERGE",
        "GRANT",
        "REVOKE",
        "EXEC",
        "CALL"
    ];

    public void ValidateOrThrow(string sql)
    {
        if (string.IsNullOrWhiteSpace(sql))
        {
            throw new InvalidOperationException("O SQL gerado está vazio.");
        }

        var normalizedSql = sql.Trim();

        if (!normalizedSql.StartsWith("SELECT", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Somente consultas SELECT são permitidas.");
        }

        if (normalizedSql.Contains(';'))
        {
            throw new InvalidOperationException("Múltiplos comandos SQL não são permitidos.");
        }

        foreach (var keyword in ForbiddenKeywords)
        {
            if (Regex.IsMatch(normalizedSql, $@"\b{keyword}\b", RegexOptions.IgnoreCase))
            {
                throw new InvalidOperationException($"O SQL contém comando proibido: {keyword}.");
            }
        }
    }
}