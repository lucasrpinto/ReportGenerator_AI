namespace Relatorios.Application.UseCases.Reports.PreviewReport;

// Resultado da pré-visualização
public sealed class PreviewReportResult
{
    public string ReportName { get; set; } = string.Empty;
    public string ReportType { get; set; } = string.Empty;
    public string Entity { get; set; } = string.Empty;
    public string Metric { get; set; } = string.Empty;

    public PreviewReportPeriodResult? Period { get; set; }

    public PreviewReportSummaryResult Summary { get; set; } = new();

    public List<string> Columns { get; set; } = new();

    public List<Dictionary<string, object?>> Rows { get; set; } = new();
}

public sealed class PreviewReportPeriodResult
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
}

public sealed class PreviewReportSummaryResult
{
    public int RowCount { get; set; }
    public decimal ValorTotal { get; set; }
}