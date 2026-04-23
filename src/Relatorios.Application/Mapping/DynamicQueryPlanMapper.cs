using System.Text.Json;
using Relatorios.Domain.DynamicQuerying;
using Relatorios.Domain.Querying;

namespace Relatorios.Application.Mapping;

public sealed class DynamicQueryPlanMapper
{
    public QueryPlan Map(DynamicQueryPlanDto dto)
    {
        var queryPlan = new QueryPlan
        {
            Source = dto.Source,
            SourceAlias = dto.SourceAlias,
            Limit = dto.Limit
        };

        foreach (var join in dto.Joins)
        {
            queryPlan.Joins.Add(new QueryJoin
            {
                Type = join.Type,
                Table = join.Table,
                Alias = join.Alias,
                On = join.On
            });
        }

        foreach (var field in dto.SelectFields)
        {
            queryPlan.SelectFields.Add(new QuerySelectField
            {
                Field = field.Field,
                Alias = field.Alias,
                Aggregation = field.Aggregation
            });
        }

        foreach (var filter in dto.Filters)
        {
            queryPlan.Filters.Add(new QueryFilter
            {
                Field = filter.Field,
                Operator = filter.Operator,
                Value = ConvertJsonElement(filter.Value)
            });
        }

        foreach (var groupBy in dto.GroupBy)
        {
            queryPlan.GroupByFields.Add(groupBy);
        }

        foreach (var orderBy in dto.OrderBy)
        {
            queryPlan.OrderByFields.Add(new QueryOrderBy
            {
                Field = orderBy.Field,
                Direction = orderBy.Direction
            });
        }

        return queryPlan;
    }

    private static object? ConvertJsonElement(object? value)
    {
        if (value is not JsonElement jsonElement)
        {
            return value;
        }

        return jsonElement.ValueKind switch
        {
            JsonValueKind.String => jsonElement.GetString(),
            JsonValueKind.Number when jsonElement.TryGetInt32(out var intValue) => intValue,
            JsonValueKind.Number when jsonElement.TryGetDecimal(out var decimalValue) => decimalValue,
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            _ => jsonElement.ToString()
        };
    }
}