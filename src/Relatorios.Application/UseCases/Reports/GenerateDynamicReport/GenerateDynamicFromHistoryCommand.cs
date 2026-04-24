using Relatorios.Contracts.Enums;

namespace Relatorios.Application.UseCases.Reports.GenerateDynamicReport;

public sealed class GenerateDynamicFromHistoryCommand
{
    public Guid HistoryId { get; set; }
    public List<ReportFormat> Formats { get; set; } = new();
}