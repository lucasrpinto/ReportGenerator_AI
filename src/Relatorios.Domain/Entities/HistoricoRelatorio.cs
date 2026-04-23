namespace Relatorios.Domain.Entities;

// Representa o histórico salvo no banco secundário
public sealed class HistoricoRelatorio
{
    public long Id { get; private set; }
    public string NomeRelatorio { get; private set; } = string.Empty;
    public DateTime? DataInicio { get; private set; }
    public DateTime? DataFim { get; private set; }
    public decimal ValorTotal { get; private set; }
    public DateTime CriadoEm { get; private set; }

    private HistoricoRelatorio()
    {
    }

    public static HistoricoRelatorio Criar(
        string nomeRelatorio,
        DateTime? dataInicio,
        DateTime? dataFim,
        decimal valorTotal)
    {
        return new HistoricoRelatorio
        {
            NomeRelatorio = nomeRelatorio,
            DataInicio = dataInicio,
            DataFim = dataFim,
            ValorTotal = valorTotal,
            CriadoEm = DateTime.UtcNow
        };
    }

    public static HistoricoRelatorio Hidratar(
        long id,
        string nomeRelatorio,
        DateTime? dataInicio,
        DateTime? dataFim,
        decimal valorTotal,
        DateTime criadoEm)
    {
        return new HistoricoRelatorio
        {
            Id = id,
            NomeRelatorio = nomeRelatorio,
            DataInicio = dataInicio,
            DataFim = dataFim,
            ValorTotal = valorTotal,
            CriadoEm = criadoEm
        };
    }
}