using Relatorios.Application.Schema;
using Relatorios.Domain.DynamicQuerying;
using Relatorios.Infrastructure.Schema;

namespace Relatorios.Infrastructure.Validation;

public sealed class DynamicQueryCatalogValidator
{
    private static readonly HashSet<string> AllowedOperators =
    [
        "=",
        ">",
        "<",
        ">=",
        "<=",
        "<>",
        "!=",
        "IS NULL",
        "IS NOT NULL",
        "LIKE",
        "ILIKE"
    ];

    public List<string> Validate(DynamicQueryPlanDto plan, SchemaCatalog catalog)
    {
        var errors = new List<string>();

        var sourceTable = catalog.Tables.FirstOrDefault(t =>
            string.Equals(t.Name, plan.Source, StringComparison.OrdinalIgnoreCase));

        if (sourceTable is null)
        {
            errors.Add($"A tabela principal '{plan.Source}' não está permitida.");
            return errors;
        }

        if (!string.Equals(sourceTable.Alias, plan.SourceAlias, StringComparison.OrdinalIgnoreCase))
        {
            errors.Add($"O alias '{plan.SourceAlias}' não é válido para a tabela '{plan.Source}'.");
        }

        foreach (var selectField in plan.SelectFields)
        {
            if (!IsAllowedField(selectField.Field, catalog))
            {
                errors.Add($"Campo de seleção não permitido: {selectField.Field}");
            }
        }

        foreach (var filter in plan.Filters)
        {
            if (!IsAllowedField(filter.Field, catalog))
            {
                errors.Add($"Campo de filtro não permitido: {filter.Field}");
            }

            if (!AllowedOperators.Contains(filter.Operator.Trim().ToUpperInvariant()))
            {
                errors.Add($"Operador não permitido: {filter.Operator}");
            }
        }

        foreach (var orderBy in plan.OrderBy)
        {
            if (!IsAllowedField(orderBy.Field, catalog))
            {
                errors.Add($"Campo de ordenação não permitido: {orderBy.Field}");
            }
        }

        foreach (var groupBy in plan.GroupBy)
        {
            if (!IsAllowedField(groupBy, catalog))
            {
                errors.Add($"Campo de agrupamento não permitido: {groupBy}");
            }
        }

        foreach (var join in plan.Joins)
        {
            var allowedJoin = sourceTable.AllowedJoins.FirstOrDefault(x =>
                string.Equals(x.Type, join.Type, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(x.Table, join.Table, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(x.Alias, join.Alias, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(NormalizeSql(x.On), NormalizeSql(join.On), StringComparison.OrdinalIgnoreCase));

            if (allowedJoin is null)
            {
                errors.Add($"Join não permitido: {join.Type} {join.Table} {join.Alias} ON {join.On}");
            }
        }

        return errors;
    }

    private static bool IsAllowedField(string field, SchemaCatalog catalog)
    {
        if (string.IsNullOrWhiteSpace(field))
        {
            return false;
        }

        field = field.Trim();

        if (field.Contains('(') || field.Contains(')'))
        {
            return false;
        }

        var parts = field.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (parts.Length != 2)
        {
            return false;
        }

        var alias = parts[0];
        var column = parts[1];

        foreach (var table in catalog.Tables)
        {
            if (!string.Equals(table.Alias, alias, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (table.Columns.Any(c => string.Equals(c.Name, column, StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }
        }

        return false;
    }

    private static string NormalizeSql(string value)
    {
        return string.Join(" ", value
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
    }
}