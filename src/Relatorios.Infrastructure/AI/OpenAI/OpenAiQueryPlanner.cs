using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Relatorios.Application.Abstractions.AI;
using System.Text.Json.Serialization;
using Relatorios.Domain.DynamicQuerying;
using Relatorios.Infrastructure.Options;
using Relatorios.Infrastructure.Schema;
using Relatorios.Application.Schema;

namespace Relatorios.Infrastructure.AI.OpenAI;

public sealed class OpenAiQueryPlanner : IOpenAiQueryPlanner
{
    private readonly HttpClient _httpClient;
    private readonly OpenAiOptions _options;
    private readonly ISchemaCatalogProvider _schemaCatalogProvider;

    public OpenAiQueryPlanner(
        HttpClient httpClient,
        IOptions<OpenAiOptions> options,
        ISchemaCatalogProvider schemaCatalogProvider)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _schemaCatalogProvider = schemaCatalogProvider;
    }

    public async Task<DynamicQueryPlanDto> PlanAsync(string prompt, CancellationToken cancellationToken)
    {
        var catalog = _schemaCatalogProvider.GetCatalog();
        var schemaText = BuildSchemaDescription(catalog);

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"{_options.BaseUrl.TrimEnd('/')}/responses");

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);

        var payload = new
        {
            model = _options.Model,
            input = new object[]
            {
                new
                {
                    role = "system",
                    content = new object[]
                    {
                        new
                        {
                            type = "input_text",
                            text =
                                $"""
                                    Você é um planejador de consultas SQL para PostgreSQL.
                                    Sua tarefa é converter o pedido do usuário em um plano estruturado de consulta.
                                    Responda obrigatoriamente chamando a função build_query_plan.
                                    Nunca gere SQL livre.
                                    Nunca invente tabelas, colunas ou joins.
                                    Use apenas o schema permitido abaixo.

                                    Regras obrigatórias:
                                    - Sempre preencha source.
                                    - Sempre preencha source_alias quando houver alias conhecido para a tabela principal.
                                    - Nunca deixe select_fields vazio.
                                    - Toda consulta deve conter pelo menos um campo em select_fields.
                                    - Quando o usuário pedir pedidos, inclua pelo menos p.id e p.total.
                                    - Quando houver cliente no pedido ou join com clientes, inclua c.nome.
                                    - Quando o usuário pedir pedidos cancelados, inclua p.cancelado_em.
                                    - Quando o usuário pedir "maior", "top", "ranking" ou "de maior valor", preencha order_by.
                                    - Quando o usuário pedir "maior valor" para pedidos, use order_by por p.total DESC.
                                    - Se a tabela principal for pedidos, use source_alias = p.
                                    - Não use tabelas fora do catálogo.
                                    - Não use colunas fora do catálogo.
                                    - Não deixe joins, filters, group_by ou order_by nulos; use array vazio quando não houver itens.
                                    - Nunca deixe select_fields vazio.

                                    Regras de agregação:
                                    - As agregações permitidas são somente SUM, COUNT e AVG.
                                    - Use aggregation vazio quando o campo não for agregado.
                                    - Quando houver agregação e também campos sem agregação, preencha group_by com todos os campos sem agregação.
                                    - Para ranking de clientes por valor total, selecione c.nome com aggregation vazio e p.total com aggregation = SUM.
                                    - Para quantidade de pedidos, use COUNT no campo p.id.
                                    - Para média de valor dos pedidos, use AVG em p.total.
                                    - Nunca use expressões SQL livres como CASE, CAST, CONCAT, subqueries, funções arbitrárias ou operações matemáticas no field.
                                    - O campo field deve conter apenas alias.coluna, por exemplo c.nome ou p.total.
                                    - Você não deve colocar SUM(p.total) no field. O field deve ser p.total e a aggregation deve ser SUM.
                                    - Quando o usuário pedir ranking, top ou maiores, preencha order_by.
                                    - Em consultas agregadas, você pode ordenar pelo alias do campo agregado.
                                    - Sempre informe limit.
                                    - Quando o usuário não informar limite, use 50.
                                    - Nunca use limit maior que 200.
                                    - Em consultas com GROUP BY, o ORDER BY deve usar o alias do campo agregado ou um campo presente em group_by.
                                    - Para ranking por valor total, use order_by no alias do campo agregado.
                                    - Exemplo: se p.total tiver aggregation = SUM e alias = total_sum, o order_by deve usar field = total_sum.
                                    - Nunca use ORDER BY com campo bruto não agrupado em consultas agregadas.

                                    Exemplo de interpretação:
                                    Pedido: "quero os 10 pedidos cancelados de maior valor"
                                    Plano esperado:
                                    - source = pedidos
                                    - source_alias = p
                                    - select_fields contendo ao menos p.id, p.total, p.cancelado_em
                                    - filters contendo p.cancelado_em IS NOT NULL
                                    - order_by contendo p.total DESC
                                    - limit = 10

                                    SCHEMA PERMITIDO:
                                    {schemaText}
                                 """
                        }
                    }
                },
                new
                {
                    role = "user",
                    content = new object[]
                    {
                        new
                        {
                            type = "input_text",
                            text = prompt
                        }
                    }
                }
            },
            tools = new object[]
            {
                new
                {
                    type = "function",
                    name = "build_query_plan",
                    description = "Monta um plano estruturado de consulta para relatórios dinâmicos",
                    strict = true,
                    parameters = new
                    {
                        type = "object",
                        additionalProperties = false,
                        properties = new
                        {
                            source = new
                            {
                                type = "string",
                                description = "Tabela principal"
                            },
                            source_alias = new
                            {
                                type = "string",
                                description = "Alias da tabela principal",
                                minLength = 1
                            },
                            joins = new
                            {
                                type = "array",
                                items = new
                                {
                                    type = "object",
                                    additionalProperties = false,
                                    properties = new
                                    {
                                        type = new { type = "string", minLength = 1 },
                                        table = new { type = "string", minLength = 1 },
                                        alias = new { type = "string", minLength = 1 },
                                        on = new { type = "string", minLength = 1 }
                                    },
                                    required = new[] { "type", "table", "alias", "on" }
                                }
                            },
                            select_fields = new
                            {
                                type = "array",
                                minItems = 1,
                                items = new
                                {
                                    type = "object",
                                    additionalProperties = false,
                                    properties = new
                                    {
                                        field = new
                                        {
                                            type = "string",
                                            minLength = 1
                                        },
                                        alias = new
                                        {
                                            type = "string"
                                        },
                                        aggregation = new
                                        {
                                            type = "string",
                                            @enum = new[] { "", "SUM", "COUNT", "AVG" }
                                        }
                                    },
                                    required = new[] { "field", "alias", "aggregation" }
                                }
                            },
                            filters = new
                            {
                                type = "array",
                                items = new
                                {
                                    type = "object",
                                    additionalProperties = false,
                                    properties = new
                                    {
                                        field = new { type = "string" },
                                        @operator = new { type = "string" },
                                        value = new
                                        {
                                            type = new[] { "string", "number", "boolean", "null" }
                                        }
                                    },
                                    required = new[] { "field", "operator", "value" }
                                }
                            },
                            group_by = new
                            {
                                type = "array",
                                items = new { type = "string" }
                            },
                            order_by = new
                            {
                                type = "array",
                                items = new
                                {
                                    type = "object",
                                    additionalProperties = false,
                                    properties = new
                                    {
                                        field = new { type = "string", minLength = 1 },
                                        direction = new
                                        {
                                            type = "string",
                                            @enum = new[] { "ASC", "DESC" }
                                        }
                                    },
                                    required = new[] { "field", "direction" }
                                }
                            },
                            limit = new
                            {
                                type = new[] { "integer", "null" }
                            }
                        },
                        required = new[]
                        {
                            "source",
                            "source_alias",
                            "joins",
                            "select_fields",
                            "filters",
                            "group_by",
                            "order_by",
                            "limit"
                        }
                    }
                }
            },
            tool_choice = new
            {
                type = "function",
                name = "build_query_plan"
            }
        };

        var json = JsonSerializer.Serialize(payload);
        request.Content = new StringContent(json, Encoding.UTF8, "application/json");

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var content = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"Erro ao chamar a OpenAI. Status: {(int)response.StatusCode} ({response.StatusCode}). Resposta: {content}");
        }

        using var document = JsonDocument.Parse(content);

        var argumentsJson = ExtractFunctionArguments(document.RootElement);

        var functionResult = JsonSerializer.Deserialize<FunctionResultDto>(
            argumentsJson,
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

        if (functionResult is null)
        {
            throw new InvalidOperationException("A OpenAI não retornou argumentos válidos para o plano de consulta.");
        }

        return new DynamicQueryPlanDto
        {
            Source = functionResult.Source,
            SourceAlias = functionResult.SourceAlias,
            Joins = functionResult.Joins.Select(x => new DynamicQueryJoinDto
            {
                Type = x.Type,
                Table = x.Table,
                Alias = x.Alias,
                On = x.On
            }).ToList(),
            SelectFields = functionResult.SelectFields.Select(x => new DynamicQuerySelectFieldDto
            {
                Field = x.Field,
                Alias = x.Alias,
                Aggregation = x.Aggregation
            }).ToList(),
            Filters = functionResult.Filters.Select(x => new DynamicQueryFilterDto
            {
                Field = x.Field,
                Operator = x.Operator,
                Value = x.Value
            }).ToList(),
            GroupBy = functionResult.GroupBy,
            OrderBy = functionResult.OrderBy.Select(x => new DynamicQueryOrderByDto
            {
                Field = x.Field,
                Direction = x.Direction
            }).ToList(),
            Limit = functionResult.Limit
        };
    }

    private static string ExtractFunctionArguments(JsonElement root)
    {
        if (!root.TryGetProperty("output", out var outputArray))
        {
            throw new InvalidOperationException("Resposta da OpenAI sem campo 'output'.");
        }

        foreach (var outputItem in outputArray.EnumerateArray())
        {
            if (!outputItem.TryGetProperty("type", out var typeElement))
            {
                continue;
            }

            var itemType = typeElement.GetString();

            if (!string.Equals(itemType, "function_call", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (!outputItem.TryGetProperty("arguments", out var argumentsElement))
            {
                throw new InvalidOperationException("Function call sem campo 'arguments'.");
            }

            return argumentsElement.GetString()
                   ?? throw new InvalidOperationException("Arguments da function call vieram vazios.");
        }

        throw new InvalidOperationException("Nenhuma function call foi retornada pela OpenAI.");
    }

    private static string BuildSchemaDescription(SchemaCatalog catalog)
    {
        var builder = new StringBuilder();

        builder.AppendLine("Tabelas:");

        foreach (var table in catalog.Tables)
        {
            builder.AppendLine($"- {table.Name} ({table.Alias}) - {table.Description}");

            foreach (var column in table.Columns)
            {
                builder.AppendLine($"  - {table.Alias}.{column.Name}: {column.Description}");
            }

            if (table.AllowedJoins.Count > 0)
            {
                builder.AppendLine("  Joins permitidos:");

                foreach (var join in table.AllowedJoins)
                {
                    builder.AppendLine($"  - {join.Type} {join.Table} {join.Alias} ON {join.On}");
                }
            }
        }

        builder.AppendLine("Regras de negócio:");

        foreach (var rule in catalog.BusinessRules)
        {
            builder.AppendLine($"- {rule}");
        }

        return builder.ToString();
    }

    private sealed class FunctionResultDto
    {
        [JsonPropertyName("source")]
        public string Source { get; set; } = string.Empty;

        [JsonPropertyName("source_alias")]
        public string SourceAlias { get; set; } = string.Empty;

        [JsonPropertyName("joins")]
        public List<JoinDto> Joins { get; set; } = new();

        [JsonPropertyName("select_fields")]
        public List<SelectFieldDto> SelectFields { get; set; } = new();

        [JsonPropertyName("filters")]
        public List<FilterDto> Filters { get; set; } = new();

        [JsonPropertyName("group_by")]
        public List<string> GroupBy { get; set; } = new();

        [JsonPropertyName("order_by")]
        public List<OrderByDto> OrderBy { get; set; } = new();

        [JsonPropertyName("limit")]
        public int? Limit { get; set; }
    }

    private sealed class JoinDto
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("table")]
        public string Table { get; set; } = string.Empty;

        [JsonPropertyName("alias")]
        public string Alias { get; set; } = string.Empty;

        [JsonPropertyName("on")]
        public string On { get; set; } = string.Empty;
    }

    private sealed class SelectFieldDto
    {
        [JsonPropertyName("field")]
        public string Field { get; set; } = string.Empty;

        [JsonPropertyName("alias")]
        public string Alias { get; set; } = string.Empty;

        [JsonPropertyName("aggregation")]
        public string Aggregation { get; set; } = string.Empty;
    }

    private sealed class FilterDto
    {
        [JsonPropertyName("field")]
        public string Field { get; set; } = string.Empty;

        [JsonPropertyName("operator")]
        public string Operator { get; set; } = string.Empty;

        [JsonPropertyName("value")]
        public JsonElement? Value { get; set; }
    }

    private sealed class OrderByDto
    {
        [JsonPropertyName("field")]
        public string Field { get; set; } = string.Empty;

        [JsonPropertyName("direction")]
        public string Direction { get; set; } = string.Empty;
    }
}