using System.Data;
using ClosedXML.Excel;
using Microsoft.Extensions.Options;
using Relatorios.Application.Abstractions.Documents;
using Relatorios.Domain.Reporting;
using Relatorios.Infrastructure.Options;

namespace Relatorios.Infrastructure.Documents;

public sealed class ClosedXmlExcelReportRenderer : IExcelReportRenderer
{
    private readonly ReportStorageOptions _storageOptions;

    public ClosedXmlExcelReportRenderer(IOptions<ReportStorageOptions> options)
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

        var fileName = $"{fileNameWithoutExtension}.xlsx";
        var filePath = Path.Combine(basePath, fileName);

        using var workbook = new XLWorkbook();

        AddDataWorksheet(workbook, data);
        AddMetadataWorksheet(workbook, intent, data);

        workbook.SaveAs(filePath);

        return Task.FromResult(filePath);
    }

    private void AddDataWorksheet(XLWorkbook workbook, DataTable data)
    {
        var worksheet = workbook.Worksheets.Add("Dados");

        if (data.Columns.Count == 0)
        {
            worksheet.Cell(1, 1).Value = "Nenhum dado retornado.";
            worksheet.Columns().AdjustToContents();
            return;
        }

        for (var columnIndex = 0; columnIndex < data.Columns.Count; columnIndex++)
        {
            var cell = worksheet.Cell(1, columnIndex + 1);
            cell.Value = data.Columns[columnIndex].ColumnName;
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.LightGray;
        }

        for (var rowIndex = 0; rowIndex < data.Rows.Count; rowIndex++)
        {
            for (var columnIndex = 0; columnIndex < data.Columns.Count; columnIndex++)
            {
                var value = data.Rows[rowIndex][columnIndex];
                worksheet.Cell(rowIndex + 2, columnIndex + 1).Value =
                    value == DBNull.Value ? string.Empty : value.ToString();
            }
        }

        var range = worksheet.Range(
            1, 1,
            Math.Max(data.Rows.Count + 1, 1),
            Math.Max(data.Columns.Count, 1));

        range.CreateTable();
        worksheet.Columns().AdjustToContents();
    }

    private void AddMetadataWorksheet(XLWorkbook workbook, ReportIntent intent, DataTable data)
    {
        var worksheet = workbook.Worksheets.Add("Metadados");

        worksheet.Cell("A1").Value = "Campo";
        worksheet.Cell("B1").Value = "Valor";
        worksheet.Cell("A1").Style.Font.Bold = true;
        worksheet.Cell("B1").Style.Font.Bold = true;

        var row = 2;
        AddMetadataRow(worksheet, row++, "ReportType", intent.ReportType);
        AddMetadataRow(worksheet, row++, "Entity", intent.Entity);
        AddMetadataRow(worksheet, row++, "Metric", intent.Metric);
        AddMetadataRow(worksheet, row++, "Dimensions", string.Join(", ", intent.Dimensions));
        AddMetadataRow(worksheet, row++, "GroupBy", string.Join(", ", intent.GroupBy));
        AddMetadataRow(worksheet, row++, "SortField", intent.Sort?.Field ?? string.Empty);
        AddMetadataRow(worksheet, row++, "SortDirection", intent.Sort?.Direction ?? string.Empty);
        AddMetadataRow(worksheet, row++, "Limit", intent.Limit?.ToString() ?? string.Empty);
        AddMetadataRow(worksheet, row++, "ConfidenceScore", intent.ConfidenceScore.ToString("0.0000"));

        if (intent.TimeRange is not null)
        {
            AddMetadataRow(worksheet, row++, "StartDate", intent.TimeRange.StartDate.ToString("yyyy-MM-dd HH:mm:ss"));
            AddMetadataRow(worksheet, row++, "EndDate", intent.TimeRange.EndDate.ToString("yyyy-MM-dd HH:mm:ss"));
        }

        AddMetadataRow(worksheet, row++, "RowsReturned", data.Rows.Count.ToString());
        AddMetadataRow(worksheet, row++, "GeneratedAt", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

        worksheet.Columns().AdjustToContents();
    }

    private static void AddMetadataRow(IXLWorksheet worksheet, int row, string field, string value)
    {
        worksheet.Cell(row, 1).Value = field;
        worksheet.Cell(row, 2).Value = value;
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