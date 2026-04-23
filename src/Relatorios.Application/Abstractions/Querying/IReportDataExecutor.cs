using System.Data;
using Relatorios.Domain.Querying;

namespace Relatorios.Application.Abstractions.Querying;

public interface IReportDataExecutor
{
    Task<DataTable> ExecuteAsync(QueryPlan queryPlan, CancellationToken cancellationToken);
}