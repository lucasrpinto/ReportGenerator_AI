using Relatorios.Domain.Entities;

namespace Relatorios.Application.Abstractions.Persistence;

public interface IReportRepository
{
    Task AddAsync(GeneratedReport report, CancellationToken cancellationToken);
    Task UpdateAsync(GeneratedReport report, CancellationToken cancellationToken);
}