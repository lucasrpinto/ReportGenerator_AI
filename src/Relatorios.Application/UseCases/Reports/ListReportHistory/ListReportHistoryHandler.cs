using Relatorios.Application.Abstractions.Persistence;

namespace Relatorios.Application.UseCases.Reports.ListReportHistory;

// Handler para consultar o histórico
public sealed class ListReportHistoryHandler
{
    private readonly IHistoricoRelatorioRepository _historicoRelatorioRepository;

    public ListReportHistoryHandler(
        IHistoricoRelatorioRepository historicoRelatorioRepository)
    {
        _historicoRelatorioRepository = historicoRelatorioRepository;
    }

    public async Task<IReadOnlyList<ListReportHistoryItemResult>> HandleAsync(
        ListReportHistoryQuery query,
        CancellationToken cancellationToken)
    {
        var historicos = await _historicoRelatorioRepository.ListAsync(
            query.DataInicio,
            query.DataFim,
            query.NomeRelatorio,
            cancellationToken);

        return historicos
            .Select(x => new ListReportHistoryItemResult
            {
                Id = x.Id,
                NomeRelatorio = x.NomeRelatorio,
                DataInicio = x.DataInicio,
                DataFim = x.DataFim,
                ValorTotal = x.ValorTotal,
                CriadoEm = x.CriadoEm
            })
            .ToList();
    }
}