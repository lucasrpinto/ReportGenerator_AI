using Relatorios.Domain.DynamicQuerying;

namespace Relatorios.Application.DynamicReports;

public sealed class DynamicReportBusinessRulesApplier
{
    private readonly DynamicDateRangeExtractor _dateRangeExtractor;

    public DynamicReportBusinessRulesApplier(DynamicDateRangeExtractor dateRangeExtractor)
    {
        _dateRangeExtractor = dateRangeExtractor;
    }

    private static string ResolveDateField(string prompt)
    {
        var normalizedPrompt = prompt.Trim().ToLowerInvariant();

        var isPaidDateRequest =
            normalizedPrompt.Contains("pago") ||
            normalizedPrompt.Contains("paga") ||
            normalizedPrompt.Contains("pagas") ||
            normalizedPrompt.Contains("pagos") ||
            normalizedPrompt.Contains("pagamento") ||
            normalizedPrompt.Contains("pagamentos");

        if (isPaidDateRequest)
        {
            return "p.pago_em";
        }

        return "p.criado_em";
    }

    private void ApplyDateFilters(DynamicQueryPlanDto plan, string prompt)
    {
        var range = _dateRangeExtractor.Extract(prompt);

        if (range is null)
        {
            return;
        }

        RemoveExistingDateFilters(plan);

        var dateField = ResolveDateField(prompt);

        plan.Filters.Add(new DynamicQueryFilterDto
        {
            Field = dateField,
            Operator = ">=",
            Value = range.Start
        });

        plan.Filters.Add(new DynamicQueryFilterDto
        {
            Field = dateField,
            Operator = "<",
            Value = range.EndExclusive
        });
    }

    private static void RemoveExistingDateFilters(DynamicQueryPlanDto plan)
    {
        plan.Filters.RemoveAll(x =>
            string.Equals(x.Field, "p.pago_em", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(x.Field, "p.criado_em", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(x.Field, "p.cancelado_em", StringComparison.OrdinalIgnoreCase));
    }
    public void Apply(DynamicQueryPlanDto plan, string prompt)
    {
        var normalizedPrompt = prompt.Trim().ToLowerInvariant();

        ApplyDateFilters(plan, prompt);

        var hasAggregation = plan.SelectFields.Any(x =>
            !string.IsNullOrWhiteSpace(x.Aggregation));

        var isAverageRequest =
            normalizedPrompt.Contains("média") ||
            normalizedPrompt.Contains("media") ||
            normalizedPrompt.Contains("médio") ||
            normalizedPrompt.Contains("medio");

        var isValueRequest =
            normalizedPrompt.Contains("valor") ||
            normalizedPrompt.Contains("venda") ||
            normalizedPrompt.Contains("vendas") ||
            normalizedPrompt.Contains("total") ||
            normalizedPrompt.Contains("liquido") ||
            normalizedPrompt.Contains("líquido") ||
            normalizedPrompt.Contains("estorno") ||
            normalizedPrompt.Contains("estornado");

        if (isAverageRequest)
        {
            ApplyAverageColumns(plan);
            return;
        }

        if (isValueRequest)
        {
            ApplySaleValueColumns(plan, normalizedPrompt);
            return;
        }

        if (!hasAggregation && plan.GroupBy.Count == 0)
        {
            EnsureCliente(plan);
        }
    }

    private static void ApplySaleValueColumns(DynamicQueryPlanDto plan, string normalizedPrompt)
    {
        EnsureCliente(plan);
        EnsureEstornoJoin(plan);

        var hasNsuRequest = normalizedPrompt.Contains("nsu");

        var orderedFields = new List<DynamicQuerySelectFieldDto>
    {
        new()
        {
            Field = "p.id",
            Alias = "id_venda",
            Aggregation = string.Empty
        },
        new()
        {
            Field = "c.nome",
            Alias = "cliente",
            Aggregation = string.Empty
        },
        new()
        {
            Field = "p.total",
            Alias = "valor_bruto",
            Aggregation = string.Empty
        },
        new()
        {
            Field = DynamicCalculatedFields.ValorEstornado,
            Alias = "valor_estornado",
            Aggregation = string.Empty
        },
        new()
        {
            Field = DynamicCalculatedFields.ValorLiquido,
            Alias = "valor_liquido",
            Aggregation = string.Empty
        }
    };

        if (hasNsuRequest || plan.SelectFields.Any(x => string.Equals(x.Field, "pt.nsu", StringComparison.OrdinalIgnoreCase)))
        {
            EnsurePagamentoTransacaoJoin(plan);

            orderedFields.Add(new DynamicQuerySelectFieldDto
            {
                Field = "pt.nsu",
                Alias = "nsu",
                Aggregation = string.Empty
            });
        }

        plan.SelectFields = orderedFields;

        if (plan.OrderBy.Count == 0)
        {
            plan.OrderBy.Add(new DynamicQueryOrderByDto
            {
                Field = "p.total",
                Direction = "DESC"
            });
        }
    }

    private static void ApplyAverageColumns(DynamicQueryPlanDto plan)
    {
        EnsureEstornoJoin(plan);

        var hasClienteGroup =
            plan.GroupBy.Any(x => string.Equals(x, "c.nome", StringComparison.OrdinalIgnoreCase)) ||
            plan.SelectFields.Any(x => string.Equals(x.Field, "c.nome", StringComparison.OrdinalIgnoreCase));

        if (hasClienteGroup)
        {
            EnsureCliente(plan);
            plan.GroupBy = ["c.nome"];

            plan.SelectFields = new List<DynamicQuerySelectFieldDto>
        {
            new()
            {
                Field = "c.nome",
                Alias = "cliente",
                Aggregation = string.Empty
            },
            new()
            {
                Field = "p.total",
                Alias = "media_bruto",
                Aggregation = "AVG"
            },
            new()
            {
                Field = DynamicCalculatedFields.ValorEstornado,
                Alias = "media_estorno",
                Aggregation = "AVG"
            },
            new()
            {
                Field = DynamicCalculatedFields.ValorLiquido,
                Alias = "media_liquido",
                Aggregation = "AVG"
            }
        };

            plan.OrderBy = new List<DynamicQueryOrderByDto>
        {
            new()
            {
                Field = "media_liquido",
                Direction = "DESC"
            }
        };

            return;
        }

        plan.GroupBy = [];

        plan.SelectFields = new List<DynamicQuerySelectFieldDto>
    {
        new()
        {
            Field = "p.total",
            Alias = "media_bruto",
            Aggregation = "AVG"
        },
        new()
        {
            Field = DynamicCalculatedFields.ValorEstornado,
            Alias = "media_estorno",
            Aggregation = "AVG"
        },
        new()
        {
            Field = DynamicCalculatedFields.ValorLiquido,
            Alias = "media_liquido",
            Aggregation = "AVG"
        }
    };

        plan.OrderBy = [];
    }

    private static void EnsurePagamentoTransacaoJoin(DynamicQueryPlanDto plan)
    {
        var hasJoin = plan.Joins.Any(x =>
            string.Equals(x.Table, "pagamentos_transacoes", StringComparison.OrdinalIgnoreCase));

        if (hasJoin)
        {
            return;
        }

        plan.Joins.Add(new DynamicQueryJoinDto
        {
            Type = "LEFT JOIN",
            Table = "pagamentos_transacoes",
            Alias = "pt",
            On = "pt.id_pedido = p.id"
        });
    }

    private static void EnsureCliente(DynamicQueryPlanDto plan)
    {
        var hasClienteJoin = plan.Joins.Any(x =>
            string.Equals(x.Table, "clientes", StringComparison.OrdinalIgnoreCase));

        if (!hasClienteJoin)
        {
            plan.Joins.Add(new DynamicQueryJoinDto
            {
                Type = "INNER JOIN",
                Table = "clientes",
                Alias = "c",
                On = "c.id = p.id_cliente"
            });
        }

        EnsureSelectField(plan, "c.nome", "cliente_nome", string.Empty);
    }

    private static void EnsureEstornoJoin(DynamicQueryPlanDto plan)
    {
        var hasEstornoJoin = plan.Joins.Any(x =>
            string.Equals(x.Alias, "pep", StringComparison.OrdinalIgnoreCase));

        if (hasEstornoJoin)
        {
            return;
        }

        plan.Joins.Add(new DynamicQueryJoinDto
        {
            Type = "LEFT JOIN",
            Table = """
                    (
                        SELECT
                            id_pedido,
                            COALESCE(SUM(valor_estornado), 0) AS valor_estornado
                        FROM pedido_estornos_parciais
                        GROUP BY id_pedido
                    )
                    """,
            Alias = "pep",
            On = "pep.id_pedido = p.id"
        });
    }

    private static void EnsureSelectField(
        DynamicQueryPlanDto plan,
        string field,
        string alias,
        string aggregation)
    {
        var exists = plan.SelectFields.Any(x =>
            string.Equals(x.Field, field, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(x.Alias, alias, StringComparison.OrdinalIgnoreCase));

        if (exists)
        {
            return;
        }

        plan.SelectFields.Add(new DynamicQuerySelectFieldDto
        {
            Field = field,
            Alias = alias,
            Aggregation = aggregation
        });
    }

    private static string BuildAlias(string baseAlias, string aggregation)
    {
        return aggregation switch
        {
            "SUM" => $"{baseAlias}_sum",
            "AVG" => $"{baseAlias}_avg",
            _ => baseAlias
        };
    }
}