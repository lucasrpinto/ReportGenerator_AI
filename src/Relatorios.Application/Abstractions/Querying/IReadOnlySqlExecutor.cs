using System.Data;

namespace Relatorios.Application.Abstractions.Querying;

public interface IReadOnlySqlExecutor
{
    Task<DataTable> ExecuteAsync(
        string sql,
        int limit,
        int offset,
        int timeoutSeconds,
        CancellationToken cancellationToken);
}