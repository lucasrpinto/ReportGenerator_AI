using Relatorios.Domain.Entities;

namespace Relatorios.Application.Abstractions.Persistence;

// Contrato para salvar e consultar o histórico do relatório
public interface IHistoricoRelatorioRepository
{
    Task AddAsync(HistoricoRelatorio historicoRelatorio, CancellationToken cancellationToken);

    Task<IReadOnlyList<HistoricoRelatorio>> ListAsync(
        DateTime? dataInicio,
        DateTime? dataFim,
        string? nomeRelatorio,
        CancellationToken cancellationToken);
}