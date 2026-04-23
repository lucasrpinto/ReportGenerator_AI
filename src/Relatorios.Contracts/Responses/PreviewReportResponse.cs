namespace Relatorios.Contracts.Responses;

// Resposta da pré-visualização para o front-end
public sealed class PreviewReportResponse
{
    public string ReportName { get; set; } = string.Empty;
    public string ReportType { get; set; } = string.Empty;
    public string Entity { get; set; } = string.Empty;
    public string Metric { get; set; } = string.Empty;

    public PreviewReportPeriodResponse? Period { get; set; }

    public PreviewReportSummaryResponse Summary { get; set; } = new();

    public List<string> Columns { get; set; } = new();

    public List<Dictionary<string, object?>> Rows { get; set; } = new();
}

public sealed class PreviewReportPeriodResponse
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
}

public sealed class PreviewReportSummaryResponse
{
    public int RowCount { get; set; }
    public decimal ValorTotal { get; set; }
}