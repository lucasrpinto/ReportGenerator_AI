using Relatorios.Application.Abstractions.Persistence;

namespace Relatorios.Application.UseCases.Reports.ListDynamicReportHistory;

public sealed class ListDynamicReportHistoryHandler
{
    private readonly IDynamicReportHistoryRepository _repository;

    public ListDynamicReportHistoryHandler(IDynamicReportHistoryRepository repository)
    {
        _repository = repository;
    }

    public async Task<List<ListDynamicReportHistoryItemResult>> HandleAsync(
        CancellationToken cancellationToken)
    {
        var histories = await _repository.ListAsync(cancellationToken);

        return histories.Select(x => new ListDynamicReportHistoryItemResult
        {
            Id = x.Id,
            SourceHistoryId = x.SourceHistoryId,
            Prompt = x.Prompt,
            Action = x.Action,
            FileName = x.FileName,
            Format = x.Format,
            RowCount = x.RowCount,
            ExecutionTimeMs = x.ExecutionTimeMs,
            CreatedAt = x.CreatedAt
        }).ToList();
    }
}