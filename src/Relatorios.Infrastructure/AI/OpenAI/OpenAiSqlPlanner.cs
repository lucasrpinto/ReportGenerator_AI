using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using Relatorios.Application.Abstractions.AI;
using Relatorios.Application.Schema;
using Relatorios.Infrastructure.Options;

namespace Relatorios.Infrastructure.AI.OpenAI;

public sealed class OpenAiSqlPlanner : IOpenAiSqlPlanner
{
    private readonly HttpClient _httpClient;
    private readonly OpenAiOptions _options;
    private readonly ISchemaCatalogProvider _schemaCatalogProvider;

    public OpenAiSqlPlanner(
        HttpClient httpClient,
        IOptions<OpenAiOptions> options,
        ISchemaCatalogProvider schemaCatalogProvider)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _schemaCatalogProvider = schemaCatalogProvider;
    }

    public async Task<string> GenerateSqlAsync(string prompt, CancellationToken cancellationToken)
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
                            text = $"""
                                Você é um gerador de SQL PostgreSQL para relatórios.

                                Sua tarefa é criar uma consulta SQL de leitura, usando apenas SELECT ou WITH ... SELECT.

                                Regras obrigatórias:
                                - Responda obrigatoriamente chamando a função build_readonly_sql.
                                - Gere apenas SQL PostgreSQL.
                                - Nunca use INSERT, UPDATE, DELETE, DROP, ALTER, CREATE, TRUNCATE, MERGE, EXEC, CALL, COPY, VACUUM, ANALYZE, SET, RESET, DO ou LOCK.
                                - Nunca use ponto e vírgula.
                                - Nunca finalize o SQL com ponto e vírgula.
                                - A resposta deve conter apenas a consulta SQL, sem markdown, sem comentários e sem ponto e vírgula final.
                                - Nunca use comentários SQL.
                                - Nunca altere dados.
                                - Nunca crie tabela.
                                - Nunca apague dados.
                                - Nunca use tabelas fora do schema permitido.
                                - Nunca use colunas fora do schema permitido.
                                - Pode usar CTE com WITH.
                                - Pode usar subqueries.
                                - Pode usar funções seguras de relatório, como COALESCE, CONCAT, STRING_AGG, TO_CHAR, MAX, MIN, SUM, COUNT, AVG e ROW_NUMBER.
                                - Pode usar window functions para relatórios como últimas compras, ranking ou top N por cliente.
                                - Não coloque LIMIT na consulta. O backend aplicará limite, paginação e timeout.
                                - Se o usuário pedir clientes que não compram há X meses, use a data da última compra para filtrar.
                                - Para clientes sem compra, considere LEFT JOIN com pedidos.
                                - Para últimas compras, use ROW_NUMBER() OVER (PARTITION BY ... ORDER BY ... DESC).
                                - A consulta precisa retornar colunas com aliases amigáveis.
                                - A consulta deve ser uma única consulta de leitura.
                                - Para itens de pedido, use a tabela de itens se ela existir no schema permitido.
                                

                                Regras específicas da tabela pedido_itens:
                                - Para nome do item/ingresso, use sempre pi.nome_ingresso.
                                - Para quantidade, use sempre pi.quantidade.
                                - Para relacionar itens com pedidos, use pi.id_pedido = p.id.
                                - Nunca use pi.nome_item.
                                - Nunca use pi.nome_produto.
                                - Nunca use pi.descricao para nome do item, salvo se nome_ingresso não existir no schema.

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
                    name = "build_readonly_sql",
                    description = "Gera SQL PostgreSQL somente leitura para relatório dinâmico",
                    strict = true,
                    parameters = new
                    {
                        type = "object",
                        additionalProperties = false,
                        properties = new
                        {
                            sql = new
                            {
                                type = "string",
                                minLength = 1,
                                description = "SQL PostgreSQL somente leitura, sem ponto e vírgula"
                            }
                        },
                        required = new[] { "sql" }
                    }
                }
            },
            tool_choice = new
            {
                type = "function",
                name = "build_readonly_sql"
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

        var functionResult = JsonSerializer.Deserialize<SqlFunctionResultDto>(
            argumentsJson,
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

        if (functionResult is null || string.IsNullOrWhiteSpace(functionResult.Sql))
        {
            throw new InvalidOperationException("A OpenAI não retornou SQL válido.");
        }

        return functionResult.Sql.Trim();
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
                builder.AppendLine("  Joins conhecidos:");

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

    private sealed class SqlFunctionResultDto
    {
        [JsonPropertyName("sql")]
        public string Sql { get; set; } = string.Empty;
    }
}