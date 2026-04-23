namespace Relatorios.Contracts.Responses;

public sealed class GenerateReportResponse
{
    public Guid ReportId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}