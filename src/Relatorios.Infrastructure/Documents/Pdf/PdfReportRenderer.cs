using System.Data;
using System.Globalization;
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

        var generatedAt = DateTime.Now;
        var title = BuildTitle(intent);
        var totalLiquido = SumColumnIfExists(data, "valor_liquido", "VALOR LIQUIDO(R$)", "valor_total", "total");

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                // Paisagem quando tiver muitas colunas
                if (data.Columns.Count > 6)
                {
                    page.Size(PageSizes.A4.Landscape());
                }
                else
                {
                    page.Size(PageSizes.A4);
                }

                page.Margin(20);
                page.DefaultTextStyle(x => x.FontSize(8));

                page.Header().Element(header =>
                {
                    header.Column(column =>
                    {
                        column.Spacing(4);

                        column.Item()
                            .Text(title)
                            .FontSize(16)
                            .Bold();

                        column.Item()
                            .Text($"Gerado em: {generatedAt:dd/MM/yyyy HH:mm}");

                        column.Item()
                            .Text($"Registros: {data.Rows.Count}");

                        if (totalLiquido > 0)
                        {
                            column.Item()
                                .Text($"Total identificado: R$ {totalLiquido:N2}")
                                .Bold();
                        }

                        if (intent.TimeRange is not null)
                        {
                            column.Item()
                                .Text($"Período: {intent.TimeRange.StartDate:dd/MM/yyyy} até {intent.TimeRange.EndDate:dd/MM/yyyy}");
                        }
                    });
                });

                page.Content().PaddingTop(12).Element(content =>
                {
                    if (data.Columns.Count == 0 || data.Rows.Count == 0)
                    {
                        content.Text("Nenhum dado encontrado para este relatório.");
                        return;
                    }

                    content.Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            foreach (DataColumn _ in data.Columns)
                            {
                                columns.RelativeColumn();
                            }
                        });

                        table.Header(header =>
                        {
                            foreach (DataColumn column in data.Columns)
                            {
                                header.Cell()
                                    .Background(Colors.Grey.Lighten2)
                                    .Border(1)
                                    .BorderColor(Colors.Grey.Lighten1)
                                    .Padding(4)
                                    .Text(FormatHeader(column.ColumnName))
                                    .FontSize(7)
                                    .Bold();
                            }
                        });

                        foreach (DataRow row in data.Rows)
                        {
                            foreach (DataColumn column in data.Columns)
                            {
                                var value = row[column];

                                table.Cell()
                                    .Border(1)
                                    .BorderColor(Colors.Grey.Lighten3)
                                    .Padding(4)
                                    .Text(FormatValue(value))
                                    .FontSize(7);
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
        if (!string.IsNullOrWhiteSpace(intent.Entity))
        {
            return $"Relatório dinâmico - {intent.Entity}";
        }

        return "Relatório dinâmico";
    }

    private static string FormatHeader(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return value
            .Replace("_", " ")
            .Trim()
            .ToUpperInvariant();
    }

    private static string FormatValue(object? value)
    {
        if (value is null || value == DBNull.Value)
        {
            return string.Empty;
        }

        return value switch
        {
            DateTime date => date.ToString("dd/MM/yyyy HH:mm"),
            decimal dec => dec.ToString("N2"),
            double dbl => dbl.ToString("N2"),
            float flt => flt.ToString("N2"),
            int integer => integer.ToString("N0"),
            long longValue => longValue.ToString("N0"),
            _ => value.ToString() ?? string.Empty
        };
    }

    private static decimal SumColumnIfExists(DataTable data, params string[] columnNames)
    {
        if (data.Rows.Count == 0 || data.Columns.Count == 0)
        {
            return 0m;
        }

        var column = data.Columns
            .Cast<DataColumn>()
            .FirstOrDefault(c => columnNames.Any(name =>
                string.Equals(c.ColumnName, name, StringComparison.OrdinalIgnoreCase)));

        if (column is null)
        {
            return 0m;
        }

        var total = 0m;

        foreach (DataRow row in data.Rows)
        {
            var value = row[column];

            if (value is null || value == DBNull.Value)
            {
                continue;
            }

            if (value is decimal dec)
            {
                total += dec;
                continue;
            }

            var text = value.ToString();

            if (decimal.TryParse(text, NumberStyles.Any, new CultureInfo("pt-BR"), out var parsedPtBr))
            {
                total += parsedPtBr;
                continue;
            }

            if (decimal.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsedInvariant))
            {
                total += parsedInvariant;
            }
        }

        return total;
    }

    private string ResolveBasePath()
    {
        if (Path.IsPathRooted(_storageOptions.BasePath))
        {
            return _storageOptions.BasePath;
        }

        return Path.Combine(AppContext.BaseDirectory, _storageOptions.BasePath);
    }
}