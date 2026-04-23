using Relatorios.Application.Abstractions.Persistence;
using Relatorios.Domain.Entities;

namespace Relatorios.Infrastructure.Repositories;

public sealed class InMemoryReportRepository : IReportRepository
{
    private static readonly List<GeneratedReport> Reports = new();

    public Task AddAsync(GeneratedReport report, CancellationToken cancellationToken)
    {
        Reports.Add(report);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(GeneratedReport report, CancellationToken cancellationToken)
    {
        var index = Reports.FindIndex(x => x.Id == report.Id);

        if (index >= 0)
        {
            Reports[index] = report;
        }

        return Task.CompletedTask;
    }
}