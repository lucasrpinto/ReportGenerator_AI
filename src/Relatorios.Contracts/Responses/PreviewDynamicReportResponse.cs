namespace Relatorios.Contracts.Responses;

public sealed class PreviewDynamicReportResponse
{
    public string Sql { get; set; } = string.Empty;
    public Guid HistoryId { get; set; }

    public int RowCount { get; set; }
    public long ExecutionTimeMs { get; set; }

    public int Page { get; set; }
    public int PageSize { get; set; }
    public int MaxPreviewRows { get; set; }
    public bool HasNextPage { get; set; }

    public List<string> Columns { get; set; } = new();
    public List<Dictionary<string, object?>> Rows { get; set; } = new();
}