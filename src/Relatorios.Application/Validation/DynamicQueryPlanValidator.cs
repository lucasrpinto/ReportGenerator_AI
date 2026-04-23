using Relatorios.Domain.DynamicQuerying;

namespace Relatorios.Application.Validation;

public sealed class DynamicQueryPlanValidator
{
    public List<string> Validate(DynamicQueryPlanDto plan)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(plan.Source))
        {
            errors.Add("A fonte principal da consulta não foi informada.");
        }

        if (string.IsNullOrWhiteSpace(plan.SourceAlias))
        {
            errors.Add("O alias da tabela principal não foi informado.");
        }

        if (plan.SelectFields.Count == 0)
        {
            errors.Add("A consulta precisa ter pelo menos um campo de seleção.");
        }

        if (plan.SelectFields.Any(x => string.IsNullOrWhiteSpace(x.Field)))
        {
            errors.Add("Todos os campos de seleção precisam informar o campo.");
        }

        if (plan.OrderBy.Any(x => string.IsNullOrWhiteSpace(x.Field)))
        {
            errors.Add("Todos os campos de ordenação precisam informar o campo.");
        }

        if (plan.OrderBy.Any(x =>
                !string.Equals(x.Direction, "ASC", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(x.Direction, "DESC", StringComparison.OrdinalIgnoreCase)))
        {
            errors.Add("A direção do ORDER BY deve ser ASC ou DESC.");
        }

        if (!plan.Limit.HasValue)
        {
            errors.Add("A consulta deve possuir limite.");
        }

        if (plan.Limit.HasValue && plan.Limit.Value <= 0)
        {
            errors.Add("O limite, quando informado, deve ser maior que zero.");
        }

        if (plan.Limit.HasValue && plan.Limit.Value > 200)
        {
            errors.Add("O limite máximo permitido nesta fase é 200.");
        }

        if (plan.Joins.Count > 5)
        {
            errors.Add("Quantidade de joins acima do limite permitido.");
        }

        if (plan.SelectFields.Any(x => (x.Aggregation ?? string.Empty).Contains('(')))
        {
            errors.Add("A agregação não deve conter expressão SQL.");
        }

        if (plan.SelectFields.Any(x => x.Field.Contains('(') || x.Field.Contains(')')))
        {
            errors.Add("O campo de seleção não deve conter expressão SQL.");
        }

        if (plan.Filters.Any(x => x.Field.Contains('(') || x.Field.Contains(')')))
        {
            errors.Add("O campo de filtro não deve conter expressão SQL.");
        }

        if (plan.GroupBy.Any(x => x.Contains('(') || x.Contains(')')))
        {
            errors.Add("O GROUP BY não deve conter expressão SQL.");
        }

        if (plan.OrderBy.Any(x => x.Field.Contains('(') || x.Field.Contains(')')))
        {
            errors.Add("O ORDER BY não deve conter expressão SQL.");
        }

        return errors;
    }
}