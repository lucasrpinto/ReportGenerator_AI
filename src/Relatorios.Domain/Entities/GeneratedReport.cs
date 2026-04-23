using Relatorios.Domain.Enums;
using Relatorios.Domain.Reporting;

namespace Relatorios.Domain.Entities;

public sealed class GeneratedReport
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string Prompt { get; private set; } = string.Empty;
    public ReportStatus Status { get; private set; } = ReportStatus.Pending;
    public ReportIntent? Intent { get; private set; }
    public string? PdfPath { get; private set; }
    public string? ExcelPath { get; private set; }

    private GeneratedReport()
    {
    }

    public static GeneratedReport Create(string prompt)
    {
        return new GeneratedReport
        {
            Prompt = prompt
        };
    }

    public void SetIntent(ReportIntent intent)
    {
        Intent = intent;
    }

    public void MarkAsProcessing()
    {
        Status = ReportStatus.Processing;
    }

    public void MarkAsCompleted(string? pdfPath, string? excelPath)
    {
        Status = ReportStatus.Completed;
        PdfPath = pdfPath;
        ExcelPath = excelPath;
    }

    public void MarkAsFailed()
    {
        Status = ReportStatus.Failed;
    }
}