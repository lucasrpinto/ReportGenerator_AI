using Microsoft.Extensions.Options;
using Npgsql;
using NpgsqlTypes;
using Relatorios.Application.Abstractions.Querying;
using Relatorios.Application.Abstractions.Security;
using Relatorios.Application.DynamicReports;
using Relatorios.Infrastructure.Options;
using System.Data;

namespace Relatorios.Infrastructure.Persistence.QueryExecution;

public sealed class PostgresReadOnlySqlExecutor : IReadOnlySqlExecutor
{
    private readonly PostgresOptions _options;
    private readonly ISqlSafetyValidator _sqlSafetyValidator;

    public PostgresReadOnlySqlExecutor(
        IOptions<PostgresOptions> options,
        ISqlSafetyValidator sqlSafetyValidator)
    {
        _options = options.Value;
        _sqlSafetyValidator = sqlSafetyValidator;
    }

    public async Task<DataTable> ExecuteAsync(
        string sql,
        int limit,
        int offset,
        int timeoutSeconds,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.ConnectionString))
        {
            throw new InvalidOperationException("A connection string do PostgreSQL não foi configurada.");
        }

        if (limit <= 0)
        {
            throw new InvalidOperationException("O limite de registros deve ser maior que zero.");
        }

        if (offset < 0)
        {
            throw new InvalidOperationException("O offset não pode ser negativo.");
        }

        if (timeoutSeconds <= 0)
        {
            throw new InvalidOperationException("O timeout deve ser maior que zero.");
        }

        var safeSql = DynamicSqlTextNormalizer.NormalizeForExecution(sql);

        _sqlSafetyValidator.ValidateOrThrow(safeSql);

        var wrappedSql = $"""
            SELECT *
            FROM (
                {safeSql}
            ) AS dynamic_report_result
            LIMIT @limit
            OFFSET @offset
            """;

        await using var connection = new NpgsqlConnection(_options.ConnectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new NpgsqlCommand(wrappedSql, connection);
        command.CommandTimeout = timeoutSeconds;

        command.Parameters.Add(new NpgsqlParameter("@limit", NpgsqlDbType.Integer)
        {
            Value = limit
        });

        command.Parameters.Add(new NpgsqlParameter("@offset", NpgsqlDbType.Integer)
        {
            Value = offset
        });

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var dataTable = new DataTable();
        dataTable.Load(reader);

        return dataTable;
    }
}