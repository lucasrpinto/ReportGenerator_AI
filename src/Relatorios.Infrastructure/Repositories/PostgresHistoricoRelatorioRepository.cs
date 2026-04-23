using Microsoft.Extensions.Options;
using Npgsql;
using Relatorios.Application.Abstractions.Persistence;
using Relatorios.Domain.Entities;
using Relatorios.Infrastructure.Options;
using System.Text;

namespace Relatorios.Infrastructure.Repositories;

// Repositório que grava e consulta o histórico no banco secundário
public sealed class PostgresHistoricoRelatorioRepository : IHistoricoRelatorioRepository
{
    private readonly ReportHistoryDatabaseOptions _options;

    public PostgresHistoricoRelatorioRepository(
        IOptions<ReportHistoryDatabaseOptions> options)
    {
        _options = options.Value;
    }

    public async Task AddAsync(
        HistoricoRelatorio historicoRelatorio,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.ConnectionString))
        {
            throw new InvalidOperationException(
                "A connection string do banco secundário não foi configurada.");
        }

        const string sql = """
            INSERT INTO report_generator.historico_relatorios
            (
                nome_relatorio,
                data_inicio,
                data_fim,
                valor_total,
                criado_em
            )
            VALUES
            (
                @nome_relatorio,
                @data_inicio,
                @data_fim,
                @valor_total,
                @criado_em
            );
            """;

        await using var connection = new NpgsqlConnection(_options.ConnectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("nome_relatorio", historicoRelatorio.NomeRelatorio);
        command.Parameters.AddWithValue("data_inicio", (object?)historicoRelatorio.DataInicio?.Date ?? DBNull.Value);
        command.Parameters.AddWithValue("data_fim", (object?)historicoRelatorio.DataFim?.Date ?? DBNull.Value);
        command.Parameters.AddWithValue("valor_total", historicoRelatorio.ValorTotal);
        command.Parameters.AddWithValue("criado_em", historicoRelatorio.CriadoEm);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<HistoricoRelatorio>> ListAsync(
    DateTime? dataInicio,
    DateTime? dataFim,
    string? nomeRelatorio,
    CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.ConnectionString))
        {
            throw new InvalidOperationException(
                "A connection string do banco secundário não foi configurada.");
        }

        var sql = new StringBuilder();
        sql.AppendLine("SELECT");
        sql.AppendLine("    id,");
        sql.AppendLine("    nome_relatorio,");
        sql.AppendLine("    data_inicio,");
        sql.AppendLine("    data_fim,");
        sql.AppendLine("    valor_total,");
        sql.AppendLine("    criado_em");
        sql.AppendLine("FROM report_generator.historico_relatorios");
        sql.AppendLine("WHERE 1 = 1");

        await using var connection = new NpgsqlConnection(_options.ConnectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new NpgsqlCommand();
        command.Connection = connection;

        if (dataInicio.HasValue)
        {
            sql.AppendLine("AND data_inicio >= @data_inicio");
            command.Parameters.AddWithValue("data_inicio", dataInicio.Value.Date);
        }

        if (dataFim.HasValue)
        {
            sql.AppendLine("AND data_fim <= @data_fim");
            command.Parameters.AddWithValue("data_fim", dataFim.Value.Date);
        }

        if (!string.IsNullOrWhiteSpace(nomeRelatorio))
        {
            sql.AppendLine("AND nome_relatorio ILIKE @nome_relatorio");
            command.Parameters.AddWithValue("nome_relatorio", $"%{nomeRelatorio.Trim()}%");
        }

        sql.AppendLine("ORDER BY criado_em DESC;");

        command.CommandText = sql.ToString();

        var historicos = new List<HistoricoRelatorio>();

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            historicos.Add(HistoricoRelatorio.Hidratar(
                id: reader.GetInt64(reader.GetOrdinal("id")),
                nomeRelatorio: reader.GetString(reader.GetOrdinal("nome_relatorio")),
                dataInicio: reader.IsDBNull(reader.GetOrdinal("data_inicio"))
                    ? null
                    : reader.GetDateTime(reader.GetOrdinal("data_inicio")),
                dataFim: reader.IsDBNull(reader.GetOrdinal("data_fim"))
                    ? null
                    : reader.GetDateTime(reader.GetOrdinal("data_fim")),
                valorTotal: reader.GetDecimal(reader.GetOrdinal("valor_total")),
                criadoEm: reader.GetDateTime(reader.GetOrdinal("criado_em"))));
        }

        return historicos;
    }
}