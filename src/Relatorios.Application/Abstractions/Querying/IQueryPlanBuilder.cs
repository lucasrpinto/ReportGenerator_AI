using Relatorios.Domain.Querying;
using Relatorios.Domain.Reporting;

namespace Relatorios.Application.Abstractions.Querying;

public interface IQueryPlanBuilder
{
    Task<QueryPlan> BuildAsync(ReportIntent intent, CancellationToken cancellationToken);
}