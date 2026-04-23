using System.Data;
using Microsoft.Extensions.Options;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Relatorios.Application.Abstractions.Documents;
using Relatorios.Domain.Reporting;
using Relatorios.Infrastructure.Options;

namespace Relatorios.Infrastructure.Documents.Pdf;

public sealed class PdfReportRenderer : IPdfReportRenderer
{
    private readonly ReportStorageOptions _storageOptions;

    public PdfReportRenderer(IOptions<ReportStorageOptions> options)
    {
        _storageOptions = options.Value;
    }

    public Task<string> RenderAsync(
        ReportIntent intent,
        DataTable data,
        string fileNameWithoutExtension,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var basePath = ResolveBasePath();
        Directory.CreateDirectory(basePath);

        var fileName = $"{fileNameWithoutExtension}.pdf";
        var filePath = Path.Combine(basePath, fileName);

        var reportTitle = BuildTitle(intent);
        var valorTotal = TryExtractValorTotal(data);
        var generatedAt = DateTime.Now;

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(20);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(9));

                page.Header().Column(column =>
                {
                    column.Spacing(4);

                    column.Item()
                        .Text(reportTitle)
                        .FontSize(16)
                        .Bold();

                    column.Item().Text($"Gerado em: {generatedAt:dd/MM/yyyy HH:mm}");

                    if (intent.TimeRange is not null)
                    {
                        column.Item()
                            .Text($"Período: {intent.TimeRange.StartDate:dd/MM/yyyy} até {intent.TimeRange.EndDate:dd/MM/yyyy}");
                    }

                    column.Item().Text($"Registros: {data.Rows.Count}");
                    column.Item().Text($"Valor total: {valorTotal:N2}");
                });

                page.Content().PaddingTop(10).Element(container =>
                {
                    if (data.Columns.Count == 0 || data.Rows.Count == 0)
                    {
                        container.Text("Nenhum dado retornado para este relatório.");
                        return;
                    }

                    container.Table(table =>
                    {
                        // largura fixa mais previsível para evitar quebra ruim / travamento visual
                        table.ColumnsDefinition(columns =>
                        {
                            for (var i = 0; i < data.Columns.Count; i++)
                                columns.RelativeColumn();
                        });

                        // cabeçalho repetido em novas páginas
                        table.Header(header =>
                        {
                            foreach (DataColumn column in data.Columns)
                            {
                                header.Cell()
                                    .Background(Colors.Grey.Lighten2)
                                    .Border(1)
                                    .BorderColor(Colors.Grey.Lighten1)
                                    .Padding(4)
                                    .Text(column.ColumnName)
                                    .FontSize(8)
                                    .Bold();
                            }
                        });

                        foreach (DataRow row in data.Rows)
                        {
                            foreach (DataColumn column in data.Columns)
                            {
                                var value = row[column];
                                var text = value == DBNull.Value ? string.Empty : FormatCellValue(value);

                                table.Cell()
                                    .Border(1)
                                    .BorderColor(Colors.Grey.Lighten2)
                                    .Padding(4)
                                    .Text(text)
                                    .FontSize(8);
                            }
                        }
                    });
                });

                page.Footer()
                    .AlignCenter()
                    .Text(text =>
                    {
                        text.Span("Página ");
                        text.CurrentPageNumber();
                        text.Span(" de ");
                        text.TotalPages();
                    });
            });
        });

        document.GeneratePdf(filePath);

        return Task.FromResult(filePath);
    }

    private static string BuildTitle(ReportIntent intent)
    {
        var reportType = (intent.ReportType ?? string.Empty).Trim().ToLowerInvariant();
        var entity = (intent.Entity ?? string.Empty).Trim().ToLowerInvariant();

        if (reportType == "total_sales")
            return "Total de vendas";

        if (reportType == "sales_ranking" &&
            (entity == "pedidos" || entity == "pedido" || entity == "vendas" || entity == "venda"))
        {
            return intent.Limit is int limit
                ? $"Top {limit} maiores vendas"
                : "Ranking de vendas";
        }

        if (reportType == "sales_ranking" &&
            (entity == "clientes" || entity == "cliente"))
        {
            return intent.Limit is int limit
                ? $"Top {limit} clientes"
                : "Ranking de clientes";
        }

        if (reportType == "single_sale")
            return "Venda específica";

        var partes = new List<string>();

        if (!string.IsNullOrWhiteSpace(intent.ReportType))
            partes.Add(intent.ReportType);

        if (!string.IsNullOrWhiteSpace(intent.Entity))
            partes.Add(intent.Entity);

        if (!string.IsNullOrWhiteSpace(intent.Metric))
            partes.Add(intent.Metric);

        return partes.Count > 0 ? string.Join(" - ", partes) : "Relatório";
    }

    private static string FormatCellValue(object value)
    {
        return value switch
        {
            DateTime dateTime => dateTime.ToString("dd/MM/yyyy HH:mm"),
            decimal dec => dec.ToString("N2"),
            double dbl => dbl.ToString("N2"),
            float flt => flt.ToString("N2"),
            _ => value.ToString() ?? string.Empty
        };
    }

    private static decimal TryExtractValorTotal(DataTable data)
    {
        if (data.Rows.Count == 0 || data.Columns.Count == 0)
            return 0m;

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
                .Cast<DataColumn>()
                .FirstOrDefault(c =>
                    c.ColumnName.Trim().Equals(nomePreferido, StringComparison.OrdinalIgnoreCase));

            if (column is null)
                continue;

            decimal soma = 0m;

            foreach (DataRow row in data.Rows)
            {
                var value = row[column];

                if (value is null || value == DBNull.Value)
                    continue;

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
                    continue;

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

    private string ResolveBasePath()
    {
        if (Path.IsPathRooted(_storageOptions.BasePath))
            return _storageOptions.BasePath;

        return Path.Combine(AppContext.BaseDirectory, _storageOptions.BasePath);
    }
}