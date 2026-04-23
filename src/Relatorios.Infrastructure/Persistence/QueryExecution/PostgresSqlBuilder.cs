using Npgsql;
using NpgsqlTypes;
using Relatorios.Domain.Querying;

namespace Relatorios.Infrastructure.Persistence.QueryExecution;

public static class PostgresSqlBuilder
{
    public static (string Sql, List<NpgsqlParameter> Parameters) Build(QueryPlan queryPlan)
    {
        if (string.IsNullOrWhiteSpace(queryPlan.Source))
        {
            throw new InvalidOperationException("A fonte de dados do QueryPlan não foi informada.");
        }

        if (queryPlan.SelectFields.Count == 0)
        {
            throw new InvalidOperationException("O QueryPlan não possui campos de seleção.");
        }

        var parameters = new List<NpgsqlParameter>();
        var sqlParts = new List<string>();

        sqlParts.Add(BuildSelectClause(queryPlan));
        sqlParts.Add(BuildFromClause(queryPlan));

        var joinClauses = BuildJoinClauses(queryPlan);
        if (joinClauses.Count > 0)
        {
            sqlParts.AddRange(joinClauses);
        }

        var whereClause = BuildWhereClause(queryPlan, parameters);
        if (!string.IsNullOrWhiteSpace(whereClause))
        {
            sqlParts.Add(whereClause);
        }

        var groupByClause = BuildGroupByClause(queryPlan);
        if (!string.IsNullOrWhiteSpace(groupByClause))
        {
            sqlParts.Add(groupByClause);
        }

        var orderByClause = BuildOrderByClause(queryPlan);
        if (!string.IsNullOrWhiteSpace(orderByClause))
        {
            sqlParts.Add(orderByClause);
        }

        if (queryPlan.Limit.HasValue && queryPlan.Limit.Value > 0)
        {
            sqlParts.Add("LIMIT @limit");
            parameters.Add(new NpgsqlParameter("@limit", NpgsqlDbType.Integer)
            {
                Value = queryPlan.Limit.Value
            });
        }

        var sql = string.Join(Environment.NewLine, sqlParts);

        return (sql, parameters);
    }

    private static string BuildSelectClause(QueryPlan queryPlan)
    {
        var selectParts = new List<string>();

        foreach (var field in queryPlan.SelectFields)
        {
            var fieldExpression = field.Field;

            if (!string.IsNullOrWhiteSpace(field.Aggregation))
            {
                fieldExpression = $"{field.Aggregation.ToUpperInvariant()}({field.Field})";
            }

            if (!string.IsNullOrWhiteSpace(field.Alias))
            {
                fieldExpression += $" AS \"{field.Alias}\"";
            }

            selectParts.Add(fieldExpression);
        }

        return $"SELECT {string.Join(", ", selectParts)}";
    }

    private static string BuildFromClause(QueryPlan queryPlan)
    {
        if (string.IsNullOrWhiteSpace(queryPlan.SourceAlias))
        {
            return $"FROM {queryPlan.Source}";
        }

        return $"FROM {queryPlan.Source} {queryPlan.SourceAlias}";
    }

    private static List<string> BuildJoinClauses(QueryPlan queryPlan)
    {
        var clauses = new List<string>();

        foreach (var join in queryPlan.Joins)
        {
            var aliasPart = string.IsNullOrWhiteSpace(join.Alias)
                ? string.Empty
                : $" {join.Alias}";

            clauses.Add($"{join.Type} {join.Table}{aliasPart} ON {join.On}");
        }

        return clauses;
    }

    private static string BuildWhereClause(
        QueryPlan queryPlan,
        List<NpgsqlParameter> parameters)
    {
        if (queryPlan.Filters.Count == 0)
        {
            return string.Empty;
        }

        var conditions = new List<string>();
        var parameterIndex = 0;

        foreach (var filter in queryPlan.Filters)
        {
            var normalizedOperator = filter.Operator.Trim().ToUpperInvariant();

            if (normalizedOperator is "IS NULL" or "IS NOT NULL")
            {
                conditions.Add($"{filter.Field} {normalizedOperator}");
                continue;
            }

            var parameterName = $"@p{parameterIndex}";
            parameterIndex++;

            conditions.Add($"{filter.Field} {filter.Operator} {parameterName}");
            parameters.Add(CreateParameter(parameterName, filter.Value));
        }

        return $"WHERE {string.Join(" AND ", conditions)}";
    }

    private static NpgsqlParameter CreateParameter(string name, object? value)
    {
        if (value is DateTime dateTime)
        {
            return new NpgsqlParameter(name, NpgsqlDbType.Timestamp)
            {
                Value = dateTime
            };
        }

        if (value is null)
        {
            return new NpgsqlParameter(name, DBNull.Value);
        }

        return new NpgsqlParameter(name, value);
    }

    private static string BuildGroupByClause(QueryPlan queryPlan)
    {
        if (queryPlan.GroupByFields.Count == 0)
        {
            return string.Empty;
        }

        return $"GROUP BY {string.Join(", ", queryPlan.GroupByFields)}";
    }

    private static string BuildOrderByClause(QueryPlan queryPlan)
    {
        if (queryPlan.OrderByFields.Count == 0)
        {
            return string.Empty;
        }

        var orderParts = queryPlan.OrderByFields
            .Select(x => $"{x.Field} {x.Direction.ToUpperInvariant()}");

        return $"ORDER BY {string.Join(", ", orderParts)}";
    }
}