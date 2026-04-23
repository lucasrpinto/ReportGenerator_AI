namespace Relatorios.Application.UseCases.Reports.ListReportHistory;

// Filtros para consulta do histórico
public sealed class ListReportHistoryQuery
{
    public DateTime? DataInicio { get; set; }
    public DateTime? DataFim { get; set; }
    public string? NomeRelatorio { get; set; }
}