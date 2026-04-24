using Relatorios.Domain.Entities;

namespace Relatorios.Application.Abstractions.Persistence;

public interface IDynamicReportHistoryRepository
{
    Task SaveAsync(DynamicReportHistory history, CancellationToken cancellationToken);

    Task<DynamicReportHistory?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken);

    Task<List<DynamicReportHistory>> ListAsync(CancellationToken cancellationToken);
}