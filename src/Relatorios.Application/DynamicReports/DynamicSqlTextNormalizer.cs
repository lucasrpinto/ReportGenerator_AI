namespace Relatorios.Application.DynamicReports;

public static class DynamicSqlTextNormalizer
{
    public static string NormalizeForExecution(string sql)
    {
        if (string.IsNullOrWhiteSpace(sql))
        {
            return string.Empty;
        }

        var normalizedSql = sql.Trim();

        // Remove apenas ponto e vírgula no final da consulta.
        // Se existir ponto e vírgula no meio, o SqlSafetyValidator continuará bloqueando.
        while (normalizedSql.EndsWith(';'))
        {
            normalizedSql = normalizedSql[..^1].TrimEnd();
        }

        return normalizedSql;
    }
}