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

    public PreviewDynamicReportHandler(
        IOpenAiSqlPlanner openAiSqlPlanner,
        IReadOnlySqlExecutor readOnlySqlExecutor,
        ISqlSafetyValidator sqlSafetyValidator,
        IDynamicReportHistoryRepository historyRepository)
    {
        _openAiSqlPlanner = openAiSqlPlanner;
        _readOnlySqlExecutor = readOnlySqlExecutor;
        _sqlSafetyValidator = sqlSafetyValidator;
        _historyRepository = historyRepository;
    }

    public async Task<PreviewDynamicReportResult> HandleAsync(
        PreviewDynamicReportCommand command,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.Prompt))
        {
            throw new InvalidOperationException("O prompt é obrigatório.");
        }

        var page = command.Page.GetValueOrDefault(1);
        var pageSize = command.PageSize.GetValueOrDefault(DynamicReportLimits.PreviewDefaultPageSize);

        if (page <= 0)
        {
            throw new InvalidOperationException("A página deve ser maior que zero.");
        }

        if (pageSize <= 0)
        {
            throw new InvalidOperationException("O tamanho da página deve ser maior que zero.");
        }

        if (pageSize > DynamicReportLimits.PreviewMaxPageSize)
        {
            pageSize = DynamicReportLimits.PreviewMaxPageSize;
        }

        var offset = (page - 1) * pageSize;

        if (offset >= DynamicReportLimits.PreviewMaxRows)
        {
            throw new InvalidOperationException(
                $"O preview permite consultar no máximo {DynamicReportLimits.PreviewMaxRows} registros.");
        }

        var remainingPreviewRows = DynamicReportLimits.PreviewMaxRows - offset;

        var limitToFetch = Math.Min(
            pageSize + 1,
            remainingPreviewRows);

        var stopwatch = Stopwatch.StartNew();

        var sql = await _openAiSqlPlanner.GenerateSqlAsync(command.Prompt, cancellationToken);

        sql = DynamicSqlTextNormalizer.NormalizeForExecution(sql);

        _sqlSafetyValidator.ValidateOrThrow(sql);

        var dataTable = await _readOnlySqlExecutor.ExecuteAsync(
            sql,
            limitToFetch,
            offset,
            DynamicReportLimits.SqlCommandTimeoutSeconds,
            cancellationToken);

        var hasNextPage = dataTable.Rows.Count > pageSize;

        if (hasNextPage)
        {
            dataTable.Rows.RemoveAt(dataTable.Rows.Count - 1);
        }

        stopwatch.Stop();

        var result = new PreviewDynamicReportResult
        {
            Sql = sql,
            RowCount = dataTable.Rows.Count,
            ExecutionTimeMs = stopwatch.ElapsedMilliseconds,
            Page = page,
            PageSize = pageSize,
            MaxPreviewRows = DynamicReportLimits.PreviewMaxRows,
            HasNextPage = hasNextPage && (offset + pageSize) < DynamicReportLimits.PreviewMaxRows
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
                page,
                pageSize
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
}