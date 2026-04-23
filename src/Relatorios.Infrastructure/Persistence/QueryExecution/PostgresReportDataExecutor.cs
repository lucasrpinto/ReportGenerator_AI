using System.Data;
using Microsoft.Extensions.Options;
using Npgsql;
using Relatorios.Application.Abstractions.Querying;
using Relatorios.Domain.Querying;
using Relatorios.Infrastructure.Options;

namespace Relatorios.Infrastructure.Persistence.QueryExecution;

public sealed class PostgresReportDataExecutor : IReportDataExecutor
{
    private readonly PostgresOptions _options;

    public PostgresReportDataExecutor(IOptions<PostgresOptions> options)
    {
        _options = options.Value;
    }

    public async Task<DataTable> ExecuteAsync(QueryPlan queryPlan, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.ConnectionString))
        {
            throw new InvalidOperationException(
                "A connection string do PostgreSQL não foi configurada.");
        }

        var (sql, parameters) = PostgresSqlBuilder.Build(queryPlan);

        await using var connection = new NpgsqlConnection(_options.ConnectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new NpgsqlCommand(sql, connection);
        command.CommandTimeout = 30;

        foreach (var parameter in parameters)
        {
            command.Parameters.Add(parameter);
        }

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var dataTable = new DataTable();
        dataTable.Load(reader);

        return dataTable;
    }
}