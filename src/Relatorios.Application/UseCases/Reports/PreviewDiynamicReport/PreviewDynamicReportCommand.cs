namespace Relatorios.Application.UseCases.Reports.PreviewDynamicReport;

public sealed class PreviewDynamicReportCommand
{
    public string Prompt { get; set; } = string.Empty;
    public int? Page { get; set; }
    public int? PageSize { get; set; }
}