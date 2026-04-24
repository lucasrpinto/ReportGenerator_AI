using System.Diagnostics;
using System.Text.Json;
using Relatorios.Application.Abstractions.Documents;
using Relatorios.Application.Abstractions.Persistence;
using Relatorios.Application.Abstractions.Querying;
using Relatorios.Application.Abstractions.Security;
using Relatorios.Application.Mapping;
using Relatorios.Contracts.Enums;
using Relatorios.Domain.DynamicQuerying;
using Relatorios.Domain.Entities;
using Relatorios.Domain.Reporting;

namespace Relatorios.Application.UseCases.Reports.GenerateDynamicReport;

public sealed class GenerateDynamicFromHistoryHandler
{
    private readonly IDynamicReportHistoryRepository _historyRepository;
    private readonly DynamicQueryPlanMapper _mapper;
    private readonly IReportDataExecutor _reportDataExecutor;
    private readonly ISqlSafetyValidator _sqlSafetyValidator;
    private readonly IQuerySqlBuilder _querySqlBuilder;
    private readonly IExcelReportRenderer _excelReportRenderer;

    public GenerateDynamicFromHistoryHandler(
        IDynamicReportHistoryRepository historyRepository,
        DynamicQueryPlanMapper mapper,
        IReportDataExecutor reportDataExecutor,
        ISqlSafetyValidator sqlSafetyValidator,
        IQuerySqlBuilder querySqlBuilder,
        IExcelReportRenderer excelReportRenderer)
    {
        _historyRepository = historyRepository;
        _mapper = mapper;
        _reportDataExecutor = reportDataExecutor;
        _sqlSafetyValidator = sqlSafetyValidator;
        _querySqlBuilder = querySqlBuilder;
        _excelReportRenderer = excelReportRenderer;
    }

    public async Task<GenerateDynamicReportResult> HandleAsync(
        GenerateDynamicFromHistoryCommand command,
        CancellationToken cancellationToken)
    {
        if (command.Formats is null || command.Formats.Count == 0)
        {
            throw new InvalidOperationException("É necessário informar ao menos um formato.");
        }

        if (!command.Formats.Contains(ReportFormat.Excel))
        {
            throw new InvalidOperationException("Nesta etapa, apenas Excel está habilitado.");
        }

        var history = await _historyRepository.GetByIdAsync(command.HistoryId, cancellationToken);

        if (history is null)
        {
            throw new InvalidOperationException("Histórico não encontrado.");
        }

        if (!string.Equals(history.Action, "preview", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Só é possível gerar arquivo a partir de um histórico de preview.");
        }

        var plan = JsonSerializer.Deserialize<DynamicQueryPlanDto>(
            history.PlanJson,
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

        if (plan is null)
        {
            throw new InvalidOperationException("Não foi possível ler o plano salvo no histórico.");
        }

        var queryPlan = _mapper.Map(plan);

        var (sql, _) = _querySqlBuilder.Build(queryPlan);

        _sqlSafetyValidator.ValidateOrThrow(sql);

        var stopwatch = Stopwatch.StartNew();

        var dataTable = await _reportDataExecutor.ExecuteAsync(queryPlan, cancellationToken);

        stopwatch.Stop();

        var fileName = $"relatorio_dinamico_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

        var reportIntent = new ReportIntent
        {
            ReportType = "dynamic_report",
            Entity = plan.Source,
            Metric = "dynamic",
            Dimensions = plan.SelectFields.Select(x => x.Field).ToList(),
            GroupBy = plan.GroupBy,
            Limit = plan.Limit
        };

        var filePath = await _excelReportRenderer.RenderAsync(
            reportIntent,
            dataTable,
            fileName,
            cancellationToken);

        await _historyRepository.SaveAsync(new DynamicReportHistory
        {
            SourceHistoryId = history.Id,
            Prompt = history.Prompt,
            PlanJson = history.PlanJson,
            Sql = sql,
            Action = "generate",
            FileName = fileName,
            Format = "Excel",
            RowCount = dataTable.Rows.Count,
            ExecutionTimeMs = stopwatch.ElapsedMilliseconds,
            CreatedAt = DateTime.Now
        }, cancellationToken);

        return new GenerateDynamicReportResult
        {
            FilePath = filePath,
            FileName = fileName,
            ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
        };
    }
}