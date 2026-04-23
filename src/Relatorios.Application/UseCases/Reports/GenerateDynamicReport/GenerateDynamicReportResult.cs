namespace Relatorios.Application.UseCases.Reports.GenerateDynamicReport;

public sealed class GenerateDynamicReportResult
{
    public string FilePath { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
}