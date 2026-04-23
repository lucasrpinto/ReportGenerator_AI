using Relatorios.Application.Abstractions.AI;
using Relatorios.Application.Abstractions.Documents;
using Relatorios.Application.Abstractions.Querying;
using Relatorios.Application.Abstractions.Security;
using Relatorios.Application.Mapping;
using Relatorios.Application.Schema;
using Relatorios.Application.Validation;
using Relatorios.Contracts.Enums;
using Relatorios.Domain.Reporting;
using System.Data;

namespace Relatorios.Application.UseCases.Reports.GenerateDynamicReport;

public sealed class GenerateDynamicReportHandler
{
    private readonly IOpenAiQueryPlanner _openAiQueryPlanner;
    private readonly DynamicQueryPlanValidator _planValidator;
    private readonly DynamicQueryCatalogValidator _catalogValidator;
    private readonly ISchemaCatalogProvider _schemaCatalogProvider;
    private readonly DynamicQueryPlanMapper _mapper;
    private readonly IReportDataExecutor _reportDataExecutor;
    private readonly ISqlSafetyValidator _sqlSafetyValidator;
    private readonly IQuerySqlBuilder _querySqlBuilder;
    private readonly IExcelReportRenderer _excelReportRenderer;

    public GenerateDynamicReportHandler(
        IOpenAiQueryPlanner openAiQueryPlanner,
        DynamicQueryPlanValidator planValidator,
        DynamicQueryCatalogValidator catalogValidator,
        ISchemaCatalogProvider schemaCatalogProvider,
        DynamicQueryPlanMapper mapper,
        IReportDataExecutor reportDataExecutor,
        ISqlSafetyValidator sqlSafetyValidator,
        IQuerySqlBuilder querySqlBuilder,
        IExcelReportRenderer excelReportRenderer)
    {
        _openAiQueryPlanner = openAiQueryPlanner;
        _planValidator = planValidator;
        _catalogValidator = catalogValidator;
        _schemaCatalogProvider = schemaCatalogProvider;
        _mapper = mapper;
        _reportDataExecutor = reportDataExecutor;
        _sqlSafetyValidator = sqlSafetyValidator;
        _querySqlBuilder = querySqlBuilder;
        _excelReportRenderer = excelReportRenderer;
    }

    public async Task<GenerateDynamicReportResult> HandleAsync(
        GenerateDynamicReportCommand command,
        CancellationToken cancellationToken)
    {
        if (command.Formats is null || command.Formats.Count == 0)
        {
            throw new InvalidOperationException("É necessário informar ao menos um formato para geração.");
        }

        if (!command.Formats.Contains(ReportFormat.Excel))
        {
            throw new InvalidOperationException("Nesta etapa, apenas o formato Excel está habilitado.");
        }

        var plan = await _openAiQueryPlanner.PlanAsync(command.Prompt, cancellationToken);

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

        Console.WriteLine("SQL gerado no generate-dynamic:");
        Console.WriteLine(sql);

        var dataTable = await _reportDataExecutor.ExecuteAsync(queryPlan, cancellationToken);

        var fileName = BuildFileName();
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

        return new GenerateDynamicReportResult
        {
            FilePath = filePath,
            FileName = fileName,
            ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
        };
    }

    private static string BuildFileName()
    {
        return $"relatorio_dinamico_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
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