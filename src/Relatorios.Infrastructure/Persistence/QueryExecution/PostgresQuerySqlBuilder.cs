using Npgsql;
using Relatorios.Application.Abstractions.Querying;
using Relatorios.Domain.Querying;

namespace Relatorios.Infrastructure.Persistence.QueryExecution;

public sealed class PostgresQuerySqlBuilder : IQuerySqlBuilder
{
    public (string Sql, List<NpgsqlParameter> Parameters) Build(QueryPlan queryPlan)
    {
        return PostgresSqlBuilder.Build(queryPlan);
    }
}