using System.Diagnostics;
using System.Text.Json;
using Relatorios.Application.Abstractions.AI;
using Relatorios.Application.Abstractions.Persistence;
using Relatorios.Application.Abstractions.Querying;
using Relatorios.Application.Abstractions.Security;
using Relatorios.Application.DynamicReports;
using Relatorios.Domain.Entities;

namespace Relatorios.Application.UseCases.Reports.PreviewDynamicReport;

public sealed class PreviewDynamicReportHandler
{
    private readonly IOpenAiSqlPlanner _openAiSqlPlanner;
    private readonly IReadOnlySqlExecutor _readOnlySqlExecutor;
    private readonly ISqlSafetyValidator _sqlSafetyValidator;
    private readonly IDynamicReportHistoryRepository _historyRepository;
    private readonly DynamicDateRangeExtractor _dateRangeExtractor;

    public PreviewDynamicReportHandler(
    IOpenAiSqlPlanner openAiSqlPlanner,
    IReadOnlySqlExecutor readOnlySqlExecutor,
    ISqlSafetyValidator sqlSafetyValidator,
    IDynamicReportHistoryRepository historyRepository,
    DynamicDateRangeExtractor dateRangeExtractor)
    {
        _openAiSqlPlanner = openAiSqlPlanner;
        _readOnlySqlExecutor = readOnlySqlExecutor;
        _sqlSafetyValidator = sqlSafetyValidator;
        _historyRepository = historyRepository;
        _dateRangeExtractor = dateRangeExtractor;
    }

    public async Task<PreviewDynamicReportResult> HandleAsync(
        PreviewDynamicReportCommand command,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.Prompt))
        {
            throw new InvalidOperationException("O prompt é obrigatório.");
        }

        var dateRange = _dateRangeExtractor.Extract(command.Prompt);

        var stopwatch = Stopwatch.StartNew();

        var sql = await _openAiSqlPlanner.GenerateSqlAsync(
            command.Prompt,
            cancellationToken);

        sql = DynamicSqlTextNormalizer.NormalizeForExecution(sql);

        _sqlSafetyValidator.ValidateOrThrow(sql);

        var dataTable = await _readOnlySqlExecutor.ExecuteAsync(
            sql,
            DynamicReportLimits.PreviewMaxRows,
            offset: 0,
            DynamicReportLimits.SqlCommandTimeoutSeconds,
            cancellationToken);

        stopwatch.Stop();

        var result = new PreviewDynamicReportResult
        {
            Sql = sql,
            RowCount = dataTable.Rows.Count,
            MaxPreviewRows = DynamicReportLimits.PreviewMaxRows,
            ExecutionTimeMs = stopwatch.ElapsedMilliseconds,
            StartDate = dateRange?.Start,
            EndDate = dateRange?.EndExclusive.AddTicks(-1),
            TotalLiquido = CalculateTotalLiquido(dataTable)
        };

        foreach (System.Data.DataColumn column in dataTable.Columns)
        {
            result.Columns.Add(column.ColumnName);
        }

        foreach (System.Data.DataRow row in dataTable.Rows)
        {
            var item = new Dictionary<string, object?>();

            foreach (System.Data.DataColumn column in dataTable.Columns)
            {
                item[column.ColumnName] = row[column] == DBNull.Value ? null : row[column];
            }

            result.Rows.Add(item);
        }

        var history = new DynamicReportHistory
        {
            Prompt = command.Prompt,
            PlanJson = JsonSerializer.Serialize(new
            {
                mode = "sql",
                maxPreviewRows = DynamicReportLimits.PreviewMaxRows
            }),
            Sql = sql,
            Action = "preview",
            FileName = null,
            Format = null,
            RowCount = result.RowCount,
            ExecutionTimeMs = result.ExecutionTimeMs,
            CreatedAt = DateTime.Now
        };

        await _historyRepository.SaveAsync(history, cancellationToken);

        result.HistoryId = history.Id;

        return result;
    }

    private static decimal CalculateTotalLiquido(System.Data.DataTable dataTable)
    {
        if (dataTable.Rows.Count == 0 || dataTable.Columns.Count == 0)
        {
            return 0m;
        }

        var preferredColumns = new[]
        {
        "total_liquido",
        "valor_liquido",
        "total_faturamento_liquido",
        "total_faturamento",
        "liquido"
    };

        var column = dataTable.Columns
            .Cast<System.Data.DataColumn>()
            .FirstOrDefault(c => preferredColumns.Any(name =>
                string.Equals(c.ColumnName, name, StringComparison.OrdinalIgnoreCase)));

        if (column is not null)
        {
            return SumColumn(dataTable, column);
        }

        var totalBrutoColumn = dataTable.Columns
            .Cast<System.Data.DataColumn>()
            .FirstOrDefault(c =>
                string.Equals(c.ColumnName, "total_bruto", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(c.ColumnName, "valor_bruto", StringComparison.OrdinalIgnoreCase));

        var valorEstornadoColumn = dataTable.Columns
            .Cast<System.Data.DataColumn>()
            .FirstOrDefault(c =>
                string.Equals(c.ColumnName, "valor_estornado", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(c.ColumnName, "total_estornado", StringComparison.OrdinalIgnoreCase));

        if (totalBrutoColumn is not null)
        {
            var totalBruto = SumColumn(dataTable, totalBrutoColumn);
            var valorEstornado = valorEstornadoColumn is null
                ? 0m
                : SumColumn(dataTable, valorEstornadoColumn);

            return totalBruto - valorEstornado;
        }

        return 0m;
    }

    private static decimal SumColumn(
        System.Data.DataTable dataTable,
        System.Data.DataColumn column)
    {
        var total = 0m;

        foreach (System.Data.DataRow row in dataTable.Rows)
        {
            var value = row[column];

            if (value is null || value == DBNull.Value)
            {
                continue;
            }

            if (value is decimal decimalValue)
            {
                total += decimalValue;
                continue;
            }

            if (value is int intValue)
            {
                total += intValue;
                continue;
            }

            if (value is long longValue)
            {
                total += longValue;
                continue;
            }

            if (value is double doubleValue)
            {
                total += Convert.ToDecimal(doubleValue);
                continue;
            }

            if (value is float floatValue)
            {
                total += Convert.ToDecimal(floatValue);
                continue;
            }

            var text = value.ToString();

            if (decimal.TryParse(
                text,
                System.Globalization.NumberStyles.Any,
                new System.Globalization.CultureInfo("pt-BR"),
                out var parsedPtBr))
            {
                total += parsedPtBr;
                continue;
            }

            if (decimal.TryParse(
                text,
                System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture,
                out var parsedInvariant))
            {
                total += parsedInvariant;
            }
        }

        return total;
    }
}