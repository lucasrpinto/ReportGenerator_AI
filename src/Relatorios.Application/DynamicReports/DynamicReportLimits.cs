namespace Relatorios.Application.DynamicReports;

public static class DynamicReportLimits
{
    public const int PreviewMaxRows = 50;
    public const int PreviewDefaultPageSize = 25;
    public const int PreviewMaxPageSize = 25;

    public const int PdfMaxRows = 5000;
    public const int ExcelMaxRows = 10000;

    public const int SqlCommandTimeoutSeconds = 120;
}