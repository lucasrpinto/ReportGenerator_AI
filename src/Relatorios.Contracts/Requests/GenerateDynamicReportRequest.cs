using Relatorios.Contracts.Enums;

namespace Relatorios.Contracts.Requests;

public sealed class GenerateDynamicReportRequest
{
    public string Prompt { get; set; } = string.Empty;
    public List<ReportFormat> Formats { get; set; } = new();
}