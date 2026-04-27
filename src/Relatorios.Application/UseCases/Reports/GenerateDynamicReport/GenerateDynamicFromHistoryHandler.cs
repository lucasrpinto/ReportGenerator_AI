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
    private readonly IPdfReportRenderer _pdfReportRenderer;

    public GenerateDynamicFromHistoryHandler(
        IDynamicReportHistoryRepository historyRepository,
        DynamicQueryPlanMapper mapper,
        IReportDataExecutor reportDataExecutor,
        ISqlSafetyValidator sqlSafetyValidator,
        IQuerySqlBuilder querySqlBuilder,
        IPdfReportRenderer pdfReportRenderer,
        IExcelReportRenderer excelReportRenderer)
    {
        _historyRepository = historyRepository;
        _mapper = mapper;
        _reportDataExecutor = reportDataExecutor;
        _sqlSafetyValidator = sqlSafetyValidator;
        _querySqlBuilder = querySqlBuilder;
        _excelReportRenderer = excelReportRenderer;
        _pdfReportRenderer = pdfReportRenderer;
    }

    public async Task<GenerateDynamicReportResult> HandleAsync(
        GenerateDynamicFromHistoryCommand command,
        CancellationToken cancellationToken)
    {
        if (command.Formats is null || command.Formats.Count == 0)
        {
            throw new InvalidOperationException("É necessário informar ao menos um formato.");
        }

        if (command.Formats.Count > 1)
        {
            throw new InvalidOperationException("Informe apenas um formato por requisição.");
        }

        var selectedFormat = command.Formats.First();

        if (selectedFormat is not ReportFormat.Excel and not ReportFormat.Pdf)
        {
            throw new InvalidOperationException("Formato não suportado. Use Excel ou PDF.");
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

        var reportIntent = new ReportIntent
        {
            ReportType = "dynamic_report",
            Entity = plan.Source,
            Metric = "dynamic",
            Dimensions = plan.SelectFields.Select(x => x.Field).ToList(),
            GroupBy = plan.GroupBy,
            Limit = plan.Limit
        };

        var generatedFile = selectedFormat switch
        {
            ReportFormat.Excel => await GenerateExcelAsync(reportIntent, dataTable, cancellationToken),
            ReportFormat.Pdf => await GeneratePdfAsync(reportIntent, dataTable, cancellationToken),
            _ => throw new InvalidOperationException("Formato não suportado.")
        };

        await _historyRepository.SaveAsync(new DynamicReportHistory
        {
            SourceHistoryId = history.Id,
            Prompt = history.Prompt,
            PlanJson = history.PlanJson,
            Sql = sql,
            Action = "generate",
            FileName = generatedFile.FileName,
            Format = generatedFile.FormatName,
            RowCount = dataTable.Rows.Count,
            ExecutionTimeMs = stopwatch.ElapsedMilliseconds,
            CreatedAt = DateTime.Now
        }, cancellationToken);

        return new GenerateDynamicReportResult
        {
            FilePath = generatedFile.FilePath,
            FileName = generatedFile.FileName,
            ContentType = generatedFile.ContentType
        };
    }

    private async Task<GeneratedDynamicFile> GenerateExcelAsync(
    ReportIntent reportIntent,
    System.Data.DataTable dataTable,
    CancellationToken cancellationToken)
    {
        // Nome base sem extensão.
        // O renderer já adiciona .xlsx.
        var fileNameWithoutExtension = BuildDynamicFileNameWithoutExtension();

        var filePath = await _excelReportRenderer.RenderAsync(
            reportIntent,
            dataTable,
            fileNameWithoutExtension,
            cancellationToken);

        return new GeneratedDynamicFile
        {
            FilePath = filePath,
            FileName = Path.GetFileName(filePath),
            ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            FormatName = "Excel"
        };
    }

    private async Task<GeneratedDynamicFile> GeneratePdfAsync(
        ReportIntent reportIntent,
        System.Data.DataTable dataTable,
        CancellationToken cancellationToken)
    {
        // Nome base sem extensão.
        // O renderer já adiciona .pdf.
        var fileNameWithoutExtension = BuildDynamicFileNameWithoutExtension();

        var filePath = await _pdfReportRenderer.RenderAsync(
            reportIntent,
            dataTable,
            fileNameWithoutExtension,
            cancellationToken);

        return new GeneratedDynamicFile
        {
            FilePath = filePath,
            FileName = Path.GetFileName(filePath),
            ContentType = "application/pdf",
            FormatName = "Pdf"
        };
    }

    private static string BuildDynamicFileNameWithoutExtension()
    {
        return $"relatorio_dinamico_{DateTime.Now:yyyyMMdd_HHmmss}";
    }

    private sealed class GeneratedDynamicFile
    {
        public string FilePath { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public string FormatName { get; set; } = string.Empty;
    }
}