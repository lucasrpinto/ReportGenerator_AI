using Relatorios.Contracts.Enums;

namespace Relatorios.Contracts.Requests;

public sealed class GenerateDynamicFromHistoryRequest
{
    public Guid HistoryId { get; set; }
    public List<ReportFormat> Formats { get; set; } = new();
}