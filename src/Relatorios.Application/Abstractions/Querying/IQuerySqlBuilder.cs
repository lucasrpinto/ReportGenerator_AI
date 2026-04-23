using Npgsql;
using Relatorios.Domain.Querying;

namespace Relatorios.Application.Abstractions.Querying;

public interface IQuerySqlBuilder
{
    (string Sql, List<NpgsqlParameter> Parameters) Build(QueryPlan queryPlan);
}