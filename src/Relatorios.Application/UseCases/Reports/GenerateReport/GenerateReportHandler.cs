using Relatorios.Application.Abstractions.AI;
using Relatorios.Application.Abstractions.Documents;
using Relatorios.Application.Abstractions.Persistence;
using Relatorios.Application.Abstractions.Querying;
using Relatorios.Application.Common;
using Relatorios.Contracts.Enums;
using Relatorios.Domain.Entities;

namespace Relatorios.Application.UseCases.Reports.GenerateReport;

public sealed class GenerateReportHandler
{
    private readonly IReportInterpreter _reportInterpreter;
    private readonly IQueryPlanBuilder _queryPlanBuilder;
    private readonly IReportDataExecutor _reportDataExecutor;
    private readonly IReportRepository _reportRepository;
    private readonly IHistoricoRelatorioRepository _historicoRelatorioRepository;
    private readonly IPdfReportRenderer _pdfReportRenderer;
    private readonly IExcelReportRenderer _excelReportRenderer;

    public GenerateReportHandler(
        IReportInterpreter reportInterpreter,
        IQueryPlanBuilder queryPlanBuilder,
        IReportDataExecutor reportDataExecutor,
        IReportRepository reportRepository,
        IHistoricoRelatorioRepository historicoRelatorioRepository,
        IPdfReportRenderer pdfReportRenderer,
        IExcelReportRenderer excelReportRenderer)
    {
        _reportInterpreter = reportInterpreter;
        _queryPlanBuilder = queryPlanBuilder;
        _reportDataExecutor = reportDataExecutor;
        _reportRepository = reportRepository;
        _historicoRelatorioRepository = historicoRelatorioRepository;
        _pdfReportRenderer = pdfReportRenderer;
        _excelReportRenderer = excelReportRenderer;
    }

    public async Task<GenerateReportResult> HandleAsync(
        GenerateReportCommand command,
        CancellationToken cancellationToken)
    {
        var report = GeneratedReport.Create(command.Prompt);

        await _reportRepository.AddAsync(report, cancellationToken);

        try
        {
            report.MarkAsProcessing();
            await _reportRepository.UpdateAsync(report, cancellationToken);

            var intent = await _reportInterpreter.InterpretAsync(command.Prompt, cancellationToken);
            report.SetIntent(intent);

            var queryPlan = await _queryPlanBuilder.BuildAsync(intent, cancellationToken);
            var data = await _reportDataExecutor.ExecuteAsync(queryPlan, cancellationToken);

            var nomeRelatorio = BuildNomeRelatorio(intent);
            var valorTotal = TryExtractValorTotal(data);

            var historicoRelatorio = HistoricoRelatorio.Criar(
                nomeRelatorio,
                intent.TimeRange?.StartDate,
                intent.TimeRange?.EndDate,
                valorTotal);

            await _historicoRelatorioRepository.AddAsync(historicoRelatorio, cancellationToken);

            if (command.Formats.Count != 1)
            {
                throw new InvalidOperationException("Selecione apenas um formato por exportação.");
            }

            var formato = command.Formats[0];
            var fileNameWithoutExtension = ReportFileNameBuilder.BuildBaseFileName(intent);

            string filePath;
            string fileName;
            string contentType;

            if (formato == ReportFormat.Pdf)
            {
                filePath = await _pdfReportRenderer.RenderAsync(intent, data, fileNameWithoutExtension, cancellationToken);
                fileName = $"{fileNameWithoutExtension}.pdf";
                contentType = "application/pdf";

                report.MarkAsCompleted(filePath, null);
            }
            else
            {
                filePath = await _excelReportRenderer.RenderAsync(intent, data, fileNameWithoutExtension, cancellationToken);
                fileName = $"{fileNameWithoutExtension}.xlsx";
                contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

                report.MarkAsCompleted(null, filePath);
            }

            await _reportRepository.UpdateAsync(report, cancellationToken);

            return new GenerateReportResult
            {
                ReportId = report.Id,
                Status = "Completed",
                Message = "Relatório gerado com sucesso.",
                FilePath = filePath,
                FileName = fileName,
                ContentType = contentType
            };
        }
        catch
        {
            report.MarkAsFailed();
            await _reportRepository.UpdateAsync(report, cancellationToken);
            throw;
        }
    }

    private static string BuildNomeRelatorio(dynamic intent)
    {
        var partes = new List<string>();

        if (!string.IsNullOrWhiteSpace(intent.ReportType))
        {
            partes.Add(intent.ReportType);
        }

        if (!string.IsNullOrWhiteSpace(intent.Entity))
        {
            partes.Add(intent.Entity);
        }

        if (!string.IsNullOrWhiteSpace(intent.Metric))
        {
            partes.Add(intent.Metric);
        }

        return partes.Count > 0
            ? string.Join(" - ", partes)
            : "Relatório gerado";
    }

    private static decimal TryExtractValorTotal(System.Data.DataTable data)
    {
        if (data.Rows.Count == 0 || data.Columns.Count == 0)
        {
            return 0m;
        }

        var nomesPreferidos = new[]
        {
        "VALOR LIQUIDO(R$)",
        "VENDAS (R$)",
        "ESTORNO (R$)",
        "valor_total",
        "valor_liquido",
        "valor_venda",
        "total",
        "valor",
        "vl_total",
        "sum",
        "soma"
    };

        foreach (var nomePreferido in nomesPreferidos)
        {
            var column = data.Columns
                .Cast<System.Data.DataColumn>()
                .FirstOrDefault(c =>
                    c.ColumnName.Trim().Equals(nomePreferido, StringComparison.OrdinalIgnoreCase));

            if (column is null)
            {
                continue;
            }

            decimal soma = 0m;

            foreach (System.Data.DataRow row in data.Rows)
            {
                var value = row[column];

                if (value is null || value == DBNull.Value)
                {
                    continue;
                }

                if (value is decimal decimalValue)
                {
                    soma += decimalValue;
                    continue;
                }

                if (value is double doubleValue)
                {
                    soma += Convert.ToDecimal(doubleValue);
                    continue;
                }

                if (value is float floatValue)
                {
                    soma += Convert.ToDecimal(floatValue);
                    continue;
                }

                if (value is int intValue)
                {
                    soma += intValue;
                    continue;
                }

                if (value is long longValue)
                {
                    soma += longValue;
                    continue;
                }

                var text = value.ToString()?.Trim();

                if (string.IsNullOrWhiteSpace(text))
                {
                    continue;
                }

                if (decimal.TryParse(
                    text,
                    System.Globalization.NumberStyles.Any,
                    new System.Globalization.CultureInfo("pt-BR"),
                    out var parsedPtBr))
                {
                    soma += parsedPtBr;
                    continue;
                }

                if (decimal.TryParse(
                    text,
                    System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out var parsedInvariant))
                {
                    soma += parsedInvariant;
                }
            }

            return soma;
        }

        return 0m;
    }
}