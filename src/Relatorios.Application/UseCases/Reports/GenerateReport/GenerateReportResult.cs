namespace Relatorios.Application.UseCases.Reports.GenerateReport;

public sealed class GenerateReportResult
{
    public Guid ReportId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;

    public string FilePath { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
}