using Relatorios.Contracts.Enums;

namespace Relatorios.Application.UseCases.Reports.GenerateDynamicReport;

public sealed class GenerateDynamicReportCommand
{
    public string Prompt { get; set; } = string.Empty;
    public List<ReportFormat> Formats { get; set; } = new();
}