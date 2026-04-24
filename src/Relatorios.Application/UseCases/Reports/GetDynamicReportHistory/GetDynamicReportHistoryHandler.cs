using Relatorios.Application.Abstractions.Persistence;

namespace Relatorios.Application.UseCases.Reports.GetDynamicReportHistory;

public sealed class GetDynamicReportHistoryHandler
{
    private readonly IDynamicReportHistoryRepository _repository;

    public GetDynamicReportHistoryHandler(IDynamicReportHistoryRepository repository)
    {
        _repository = repository;
    }

    public async Task<GetDynamicReportHistoryResult?> HandleAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var history = await _repository.GetByIdAsync(id, cancellationToken);

        if (history is null)
        {
            return null;
        }

        return new GetDynamicReportHistoryResult
        {
            Id = history.Id,
            SourceHistoryId = history.SourceHistoryId,
            Prompt = history.Prompt,
            PlanJson = history.PlanJson,
            Sql = history.Sql,
            Action = history.Action,
            FileName = history.FileName,
            Format = history.Format,
            RowCount = history.RowCount,
            ExecutionTimeMs = history.ExecutionTimeMs,
            CreatedAt = history.CreatedAt
        };
    }
}