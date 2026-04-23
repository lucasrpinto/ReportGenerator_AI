namespace Relatorios.Contracts.Responses;

public sealed class PreviewDynamicReportResponse
{
    public string Sql { get; set; } = string.Empty;
    public int RowCount { get; set; }
    public long ExecutionTimeMs { get; set; }
    public List<string> Columns { get; set; } = new();
    public List<Dictionary<string, object?>> Rows { get; set; } = new();
}