using Microsoft.Extensions.Options;
using Npgsql;
using NpgsqlTypes;
using Relatorios.Application.Abstractions.Persistence;
using Relatorios.Domain.Entities;
using Relatorios.Infrastructure.Options;

namespace Relatorios.Infrastructure.Repositories;

public sealed class PostgresDynamicReportHistoryRepository : IDynamicReportHistoryRepository
{
    private readonly ReportHistoryDatabaseOptions _options;

    public PostgresDynamicReportHistoryRepository(
        IOptions<ReportHistoryDatabaseOptions> options)
    {
        _options = options.Value;
    }

    public async Task<DynamicReportHistory?> GetByIdAsync(
    Guid id,
    CancellationToken cancellationToken)
    {
        await using var connection = new NpgsqlConnection(_options.ConnectionString);
        await connection.OpenAsync(cancellationToken);

        const string sql = """
        SELECT
            id,
            source_history_id,
            prompt,
            plan_json,
            sql,
            action,
            file_name,
            format,
            row_count,
            execution_time_ms,
            created_at
        FROM dynamic_report_history
        WHERE id = @id
        LIMIT 1;
        """;

        await using var command = new NpgsqlCommand(sql, connection);

        command.Parameters.Add(new NpgsqlParameter("@id", NpgsqlDbType.Uuid)
        {
            Value = id
        });

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new DynamicReportHistory
        {
            Id = reader.GetGuid(reader.GetOrdinal("id")),
            SourceHistoryId = reader.IsDBNull(reader.GetOrdinal("source_history_id"))
                ? null
                : reader.GetGuid(reader.GetOrdinal("source_history_id")),
            Prompt = reader.GetString(reader.GetOrdinal("prompt")),
            PlanJson = reader.GetString(reader.GetOrdinal("plan_json")),
            Sql = reader.GetString(reader.GetOrdinal("sql")),
            Action = reader.GetString(reader.GetOrdinal("action")),
            FileName = reader.IsDBNull(reader.GetOrdinal("file_name"))
                ? null
                : reader.GetString(reader.GetOrdinal("file_name")),
            Format = reader.IsDBNull(reader.GetOrdinal("format"))
                ? null
                : reader.GetString(reader.GetOrdinal("format")),
            RowCount = reader.GetInt32(reader.GetOrdinal("row_count")),
            ExecutionTimeMs = reader.GetInt64(reader.GetOrdinal("execution_time_ms")),
            CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at"))
        };
    }

    public async Task<List<DynamicReportHistory>> ListAsync(CancellationToken cancellationToken)
    {
        var result = new List<DynamicReportHistory>();

        await using var connection = new NpgsqlConnection(_options.ConnectionString);
        await connection.OpenAsync(cancellationToken);

        const string sql = """
        SELECT
            id,
            source_history_id,
            prompt,
            plan_json,
            sql,
            action,
            file_name,
            format,
            row_count,
            execution_time_ms,
            created_at
        FROM dynamic_report_history
        ORDER BY created_at DESC
        LIMIT 100;
        """;

        await using var command = new NpgsqlCommand(sql, connection);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            result.Add(new DynamicReportHistory
            {
                Id = reader.GetGuid(reader.GetOrdinal("id")),
                SourceHistoryId = reader.IsDBNull(reader.GetOrdinal("source_history_id"))
                    ? null
                    : reader.GetGuid(reader.GetOrdinal("source_history_id")),
                Prompt = reader.GetString(reader.GetOrdinal("prompt")),
                PlanJson = reader.GetString(reader.GetOrdinal("plan_json")),
                Sql = reader.GetString(reader.GetOrdinal("sql")),
                Action = reader.GetString(reader.GetOrdinal("action")),
                FileName = reader.IsDBNull(reader.GetOrdinal("file_name"))
                    ? null
                    : reader.GetString(reader.GetOrdinal("file_name")),
                Format = reader.IsDBNull(reader.GetOrdinal("format"))
                    ? null
                    : reader.GetString(reader.GetOrdinal("format")),
                RowCount = reader.GetInt32(reader.GetOrdinal("row_count")),
                ExecutionTimeMs = reader.GetInt64(reader.GetOrdinal("execution_time_ms")),
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at"))
            });
        }

        return result;
    }

    public async Task SaveAsync(
        DynamicReportHistory history,
        CancellationToken cancellationToken)
    {
        await using var connection = new NpgsqlConnection(_options.ConnectionString);
        await connection.OpenAsync(cancellationToken);

        const string sql = """
                            INSERT INTO dynamic_report_history
                            (
                                id,
                                source_history_id,
                                prompt,
                                plan_json,
                                sql,
                                action,
                                file_name,
                                format,
                                row_count,
                                execution_time_ms,
                                created_at
                            )
                            VALUES
                            (
                                @id,
                                @source_history_id,
                                @prompt,
                                @plan_json,
                                @sql,
                                @action,
                                @file_name,
                                @format,
                                @row_count,
                                @execution_time_ms,
                                @created_at
                            );
                            """;

        await using var command = new NpgsqlCommand(sql, connection);

        command.Parameters.Add(new NpgsqlParameter("@id", NpgsqlDbType.Uuid) { Value = history.Id });
        command.Parameters.Add(new NpgsqlParameter("@prompt", NpgsqlDbType.Text) { Value = history.Prompt });
        command.Parameters.Add(new NpgsqlParameter("@plan_json", NpgsqlDbType.Jsonb) { Value = history.PlanJson });
        command.Parameters.Add(new NpgsqlParameter("@sql", NpgsqlDbType.Text) { Value = history.Sql });
        command.Parameters.Add(new NpgsqlParameter("@action", NpgsqlDbType.Varchar) { Value = history.Action });
        command.Parameters.Add(new NpgsqlParameter("@file_name", NpgsqlDbType.Varchar) { Value = (object?)history.FileName ?? DBNull.Value });
        command.Parameters.Add(new NpgsqlParameter("@format", NpgsqlDbType.Varchar) { Value = (object?)history.Format ?? DBNull.Value });
        command.Parameters.Add(new NpgsqlParameter("@row_count", NpgsqlDbType.Integer) { Value = history.RowCount });
        command.Parameters.Add(new NpgsqlParameter("@execution_time_ms", NpgsqlDbType.Bigint) { Value = history.ExecutionTimeMs });
        command.Parameters.Add(new NpgsqlParameter("@created_at", NpgsqlDbType.Timestamp)
        {
            Value = DateTime.SpecifyKind(history.CreatedAt, DateTimeKind.Unspecified)
        });
        command.Parameters.Add(new NpgsqlParameter("@source_history_id", NpgsqlDbType.Uuid)
        {
            Value = (object?)history.SourceHistoryId ?? DBNull.Value
        });

        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}