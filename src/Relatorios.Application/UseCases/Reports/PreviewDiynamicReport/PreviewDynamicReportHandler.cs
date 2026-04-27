using Relatorios.Application.Abstractions.AI;
using Relatorios.Application.Abstractions.Persistence;
using Relatorios.Application.Abstractions.Querying;
using Relatorios.Application.Abstractions.Security;
using Relatorios.Application.DynamicReports;
using Relatorios.Application.Mapping;
using Relatorios.Application.Schema;
using Relatorios.Application.Validation;
using Relatorios.Domain.Entities;
using System.Diagnostics;
using System.Text.Json;

namespace Relatorios.Application.UseCases.Reports.PreviewDynamicReport;

public sealed class PreviewDynamicReportHandler
{
    private readonly IOpenAiQueryPlanner _openAiQueryPlanner;
    private readonly DynamicQueryPlanValidator _planValidator;
    private readonly DynamicQueryCatalogValidator _catalogValidator;
    private readonly ISchemaCatalogProvider _schemaCatalogProvider;
    private readonly DynamicQueryPlanMapper _mapper;
    private readonly IReportDataExecutor _reportDataExecutor;
    private readonly ISqlSafetyValidator _sqlSafetyValidator;
    private readonly IQuerySqlBuilder _querySqlBuilder;
    private readonly IDynamicReportHistoryRepository _historyRepository;
    private readonly DynamicReportBusinessRulesApplier _businessRulesApplier;

    public PreviewDynamicReportHandler(
        IOpenAiQueryPlanner openAiQueryPlanner,
        DynamicQueryPlanValidator planValidator,
        DynamicQueryCatalogValidator catalogValidator,
        ISchemaCatalogProvider schemaCatalogProvider,
        DynamicQueryPlanMapper mapper,
        IReportDataExecutor reportDataExecutor,
        ISqlSafetyValidator sqlSafetyValidator,
        IDynamicReportHistoryRepository historyRepository,
        DynamicReportBusinessRulesApplier businessRulesApplier,
        IQuerySqlBuilder querySqlBuilder)
    {
        _openAiQueryPlanner = openAiQueryPlanner;
        _planValidator = planValidator;
        _catalogValidator = catalogValidator;
        _schemaCatalogProvider = schemaCatalogProvider;
        _mapper = mapper;
        _reportDataExecutor = reportDataExecutor;
        _sqlSafetyValidator = sqlSafetyValidator;
        _querySqlBuilder = querySqlBuilder;
        _historyRepository = historyRepository;
        _businessRulesApplier = businessRulesApplier;
    }

    public async Task<PreviewDynamicReportResult> HandleAsync(
    PreviewDynamicReportCommand command,
    CancellationToken cancellationToken)
    {
        // Conta o tempo total do preview desde o início do processamento
        var stopwatch = Stopwatch.StartNew();

        var plan = await _openAiQueryPlanner.PlanAsync(command.Prompt, cancellationToken);

        _businessRulesApplier.Apply(plan, command.Prompt);

        ApplySafetyDefaults(plan);

        var structuralErrors = _planValidator.Validate(plan);
        if (structuralErrors.Count > 0)
        {
            throw new InvalidOperationException(string.Join(" | ", structuralErrors));
        }

        var catalog = _schemaCatalogProvider.GetCatalog();

        var catalogErrors = _catalogValidator.Validate(plan, catalog);
        if (catalogErrors.Count > 0)
        {
            throw new InvalidOperationException(string.Join(" | ", catalogErrors));
        }

        var queryPlan = _mapper.Map(plan);

        var (sql, _) = _querySqlBuilder.Build(queryPlan);

        _sqlSafetyValidator.ValidateOrThrow(sql);

        var dataTable = await _reportDataExecutor.ExecuteAsync(queryPlan, cancellationToken);

        var result = new PreviewDynamicReportResult
        {
            Sql = sql,
            RowCount = dataTable.Rows.Count
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

        // Para salvar no histórico o tempo total do preview
        stopwatch.Stop();

        result.ExecutionTimeMs = stopwatch.ElapsedMilliseconds;

        var history = new DynamicReportHistory
        {
            Prompt = command.Prompt,
            PlanJson = JsonSerializer.Serialize(plan),
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

    private static void ApplySafetyDefaults(Relatorios.Domain.DynamicQuerying.DynamicQueryPlanDto plan)
    {
        const int defaultLimit = 50;
        const int maxLimit = 200;

        if (!plan.Limit.HasValue)
        {
            plan.Limit = defaultLimit;
        }

        if (plan.Limit.Value > maxLimit)
        {
            plan.Limit = maxLimit;
        }

        var hasAggregation = plan.SelectFields.Any(x =>
            !string.IsNullOrWhiteSpace(x.Aggregation));

        var hasFilters = plan.Filters.Count > 0;

        if (!hasFilters && !hasAggregation && plan.Limit.Value > 100)
        {
            plan.Limit = 100;
        }
    }
}