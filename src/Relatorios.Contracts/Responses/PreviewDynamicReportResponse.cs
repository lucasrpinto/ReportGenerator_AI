namespace Relatorios.Contracts.Responses;

public sealed class PreviewDynamicReportResponse
{
    public string Sql { get; set; } = string.Empty;
    public Guid HistoryId { get; set; }

    public int RowCount { get; set; }
    public int MaxPreviewRows { get; set; }

    public long ExecutionTimeMs { get; set; }

    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }

    public decimal TotalLiquido { get; set; }

    public List<string> Columns { get; set; } = new();
    public List<Dictionary<string, object?>> Rows { get; set; } = new();
}