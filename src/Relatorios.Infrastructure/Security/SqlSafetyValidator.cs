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
        "CALL",
        "COPY",
        "VACUUM",
        "ANALYZE",
        "SET",
        "RESET",
        "DO",
        "LOCK"
    ];

    private static readonly string[] ForbiddenFunctions =
    [
        "pg_sleep",
        "dblink",
        "lo_import",
        "lo_export"
    ];

    public void ValidateOrThrow(string sql)
    {
        if (string.IsNullOrWhiteSpace(sql))
        {
            throw new InvalidOperationException("O SQL gerado está vazio.");
        }

        var normalizedSql = sql.Trim();

        if (normalizedSql.Contains(';'))
        {
            throw new InvalidOperationException("Múltiplos comandos SQL não são permitidos.");
        }

        if (normalizedSql.Contains("--") || normalizedSql.Contains("/*") || normalizedSql.Contains("*/"))
        {
            throw new InvalidOperationException("Comentários SQL não são permitidos.");
        }

        var startsWithSelect =
            normalizedSql.StartsWith("SELECT", StringComparison.OrdinalIgnoreCase);

        var startsWithWith =
            normalizedSql.StartsWith("WITH", StringComparison.OrdinalIgnoreCase);

        if (!startsWithSelect && !startsWithWith)
        {
            throw new InvalidOperationException("Somente consultas SELECT ou WITH são permitidas.");
        }

        foreach (var keyword in ForbiddenKeywords)
        {
            if (Regex.IsMatch(normalizedSql, $@"\b{keyword}\b", RegexOptions.IgnoreCase))
            {
                throw new InvalidOperationException($"O SQL contém comando proibido: {keyword}.");
            }
        }

        foreach (var function in ForbiddenFunctions)
        {
            if (Regex.IsMatch(normalizedSql, $@"\b{function}\b", RegexOptions.IgnoreCase))
            {
                throw new InvalidOperationException($"O SQL contém função proibida: {function}.");
            }
        }
    }
}