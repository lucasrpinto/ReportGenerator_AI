namespace Relatorios.Domain.Reporting;

public sealed class ReportIntent
{
    public string ReportType { get; set; } = string.Empty;
    public string Entity { get; set; } = string.Empty;
    public string Metric { get; set; } = string.Empty;
    public List<string> Dimensions { get; set; } = new();
    public List<string> GroupBy { get; set; } = new();
    public SortDefinition? Sort { get; set; }
    public int? Limit { get; set; }
    public TimeRange? TimeRange { get; set; }
    public decimal ConfidenceScore { get; set; }
}

public sealed class SortDefinition
{
    public string Field { get; set; } = string.Empty;
    public string Direction { get; set; } = "DESC";
}

public sealed class TimeRange
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
}