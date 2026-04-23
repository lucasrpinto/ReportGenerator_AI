namespace Relatorios.Application.UseCases.Reports.ListReportHistory;

// Item retornado no histórico
public sealed class ListReportHistoryItemResult
{
    public long Id { get; set; }
    public string NomeRelatorio { get; set; } = string.Empty;
    public DateTime? DataInicio { get; set; }
    public DateTime? DataFim { get; set; }
    public decimal ValorTotal { get; set; }
    public DateTime CriadoEm { get; set; }
}