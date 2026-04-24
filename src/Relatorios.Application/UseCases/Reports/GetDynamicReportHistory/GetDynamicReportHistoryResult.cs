namespace Relatorios.Application.UseCases.Reports.GetDynamicReportHistory;

public sealed class GetDynamicReportHistoryResult
{
    public Guid Id { get; set; }
    public Guid? SourceHistoryId { get; set; }

    public string Prompt { get; set; } = string.Empty;
    public string PlanJson { get; set; } = string.Empty;
    public string Sql { get; set; } = string.Empty;

    public string Action { get; set; } = string.Empty;
    public string? FileName { get; set; }
    public string? Format { get; set; }

    public int RowCount { get; set; }
    public long ExecutionTimeMs { get; set; }

    public DateTime CreatedAt { get; set; }
}