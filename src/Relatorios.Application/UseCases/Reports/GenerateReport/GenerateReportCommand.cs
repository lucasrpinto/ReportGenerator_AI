using Relatorios.Contracts.Enums;

namespace Relatorios.Application.UseCases.Reports.GenerateReport;

public sealed class GenerateReportCommand
{
    public string Prompt { get; set; } = string.Empty;
    public List<ReportFormat> Formats { get; set; } = new();
}