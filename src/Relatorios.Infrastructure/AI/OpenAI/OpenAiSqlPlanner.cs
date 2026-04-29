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

                                    Responda obrigatoriamente chamando a função build_readonly_sql.

                                    A consulta gerada será validada, paginada e limitada pelo backend antes da execução.

                                    Regras obrigatórias de segurança:
                                    - Gere apenas SQL PostgreSQL.
                                    - Use somente SELECT ou WITH ... SELECT.
                                    - Nunca use INSERT.
                                    - Nunca use UPDATE.
                                    - Nunca use DELETE.
                                    - Nunca use DROP.
                                    - Nunca use ALTER.
                                    - Nunca use CREATE.
                                    - Nunca use TRUNCATE.
                                    - Nunca use MERGE.
                                    - Nunca use EXEC.
                                    - Nunca use CALL.
                                    - Nunca use COPY.
                                    - Nunca use VACUUM.
                                    - Nunca use ANALYZE.
                                    - Nunca use SET.
                                    - Nunca use RESET.
                                    - Nunca use DO.
                                    - Nunca use LOCK.
                                    - Nunca use ponto e vírgula.
                                    - Nunca finalize o SQL com ponto e vírgula.
                                    - Nunca use comentários SQL.
                                    - Nunca altere dados.
                                    - Nunca crie tabelas.
                                    - Nunca apague dados.
                                    - Nunca use tabelas fora do schema permitido.
                                    - Nunca use colunas fora do schema permitido.
                                    - Nunca invente nomes de tabelas.
                                    - Nunca invente nomes de colunas.
                                    - A resposta deve conter apenas a consulta SQL dentro do argumento sql da função.
                                    - Não retorne markdown.
                                    - Não retorne explicações.
                                    - Não retorne ```sql.

                                    Regras obrigatórias de seleção de campos:
                                    - Não retorne colunas extras sem o usuário pedir.
                                    - Não retorne dados de telefone, celular ou e-mail, exceto quando o usuário pedir explicitamente telefone, celular, e-mail, email, contato ou dados de contato.
                                    - Não retorne itens, produtos, ingressos ou dados de pedido_itens, exceto quando o usuário pedir explicitamente itens, produtos, ingressos, itens comprados ou itens das últimas compras.
                                    - Não faça JOIN com pedido_itens se o usuário não pedir itens, produtos ou ingressos.
                                    - Não faça JOIN com tabelas de contato extras se o usuário não pedir dados de contato.
                                    - Para relatórios comuns de vendas ou pedidos, quando o usuário não especificar campos adicionais, retorne somente:
                                      p.id AS pedido_id,
                                      c.nome AS cliente_nome,
                                      p.total AS total_bruto,
                                      valor_estornado,
                                      total_liquido.
                                    - Para valor_estornado, use COALESCE do total estornado do pedido.
                                    - Para total_liquido, use p.total menos o valor estornado.
                                    - Quando usar pedido_estornos_parciais, agregue os estornos por pedido em uma CTE antes de juntar com pedidos, para evitar duplicar valores.

                                    Regras sobre LIMIT, paginação e volume:
                                    - Não coloque LIMIT na consulta.
                                    - Não coloque OFFSET na consulta.
                                    - O backend aplicará limite, paginação e timeout.
                                    - O preview será limitado pelo backend.
                                    - A exportação PDF será limitada pelo backend.
                                    - A exportação Excel será limitada pelo backend.

                                    Regras sobre datas:
                                    - Quando o usuário pedir "últimas compras", "última compra", "não compraram há X dias", "não compraram há X meses", "clientes inativos", "sem comprar" ou expressões parecidas, use COALESCE(p.pago_em, p.criado_em) como data da compra.
                                    - Não filtre p.pago_em IS NOT NULL, exceto se o usuário pedir explicitamente "compras pagas", "vendas pagas", "pedidos pagos" ou "pagamentos".
                                    - Para relatórios de vendas pagas, use p.pago_em.
                                    - Para relatórios de pedidos criados, use p.criado_em.
                                    - Para relatórios de pedidos cancelados, use p.cancelado_em.
                                    - Para relatórios de pedidos atualizados, use p.atualizado_em.
                                    - Para clientes inativos, considere a última compra pelo maior valor de COALESCE(p.pago_em, p.criado_em).
                                    - Quando comparar datas relativas, use now() ou CURRENT_DATE conforme fizer sentido.
                                    - Para "faz X dias", use INTERVAL 'X days'.
                                    - Para "faz X meses", use INTERVAL 'X months'.

                                    Regras sobre clientes inativos:
                                    - Quando o usuário pedir clientes que não compraram há determinado período, use a tabela clientes como base principal.
                                    - Use LEFT JOIN com pedidos para não perder clientes sem compras.
                                    - Considere apenas clientes com c.deletado_em IS NULL quando essa coluna existir.
                                    - Considere apenas pedidos com p.deletado_em IS NULL quando essa coluna existir.
                                    - A última compra deve ser calculada por cliente.
                                    - Clientes sem compra devem aparecer quando o pedido do usuário for sobre clientes que não compraram ou clientes inativos.
                                    - Para clientes sem compra, a data da última compra pode retornar nula.
                                    - Para total de faturamento, some os pedidos do cliente e desconte estornos quando a tabela de estornos estiver disponível.
                                    - Quando houver estorno, relacione pedido_estornos_parciais com pedidos usando pep.id_pedido = p.id.

                                    Regras sobre últimas compras:
                                    - Quando o usuário pedir últimas N compras, use ROW_NUMBER() OVER (PARTITION BY p.id_cliente ORDER BY COALESCE(p.pago_em, p.criado_em) DESC).
                                    - Use CTE para separar os pedidos ranqueados.
                                    - Filtre rn <= N para pegar as últimas N compras.
                                    - Quando o usuário pedir itens das últimas compras, agregue os itens por pedido antes de agregar por cliente.
                                    - Use STRING_AGG para juntar os itens em texto amigável.
                                    - Quando possível, retorne uma coluna para cada compra, por exemplo itens_ultima_compra e itens_segunda_ultima_compra.
                                    - Se o usuário pedir últimas 3 compras, retorne dados suficientes para identificar as 3 compras.
                                    - Se o usuário pedir últimas 2 compras, retorne dados suficientes para identificar as 2 compras.

                                    Regras específicas da tabela pedido_itens:
                                    - Só use a tabela pedido_itens quando o usuário pedir explicitamente itens, produtos, ingressos, itens comprados ou itens das últimas compras.
                                    - Para nome do item ou ingresso, use sempre pi.nome_ingresso.
                                    - Para quantidade, use sempre pi.quantidade.
                                    - Para relacionar itens com pedidos, use pi.id_pedido = p.id.
                                    - Nunca use pi.nome_item.
                                    - Nunca use pi.nome_produto.
                                    - Nunca use pi.descricao para nome do item, salvo se nome_ingresso não existir no schema.
                                    - Quando agregar item e quantidade, use um texto como pi.nome_ingresso || ' x ' || pi.quantidade::text.
                                    - Quando pedido_itens tiver deletado_em, filtre pi.deletado_em IS NULL.

                                    Regras sobre faturamento:
                                    - Para faturamento bruto, use SUM(p.total).
                                    - Para faturamento líquido, use SUM(p.total) menos os valores estornados.
                                    - Quando usar pedido_estornos_parciais, agregue os estornos por pedido antes de juntar com pedidos, para evitar duplicar valores.
                                    - Se a tabela pedido_estornos_parciais tiver valor_estornado, use essa coluna.
                                    - Use COALESCE para evitar valores nulos em somas.
                                    - Quando o usuário pedir total de faturamento, prefira retornar uma coluna com alias total_faturamento ou total_faturamento_liquido.

                                    Regras sobre joins:
                                    - Use apenas joins compatíveis com o schema permitido.
                                    - Para relacionar pedidos com clientes, use p.id_cliente = c.id.
                                    - Para relacionar itens com pedidos, use pi.id_pedido = p.id.
                                    - Para relacionar estornos com pedidos, use pep.id_pedido = p.id.
                                    - Use LEFT JOIN quando o objetivo for manter clientes mesmo sem pedidos, sem itens ou sem estornos.
                                    - Use INNER JOIN apenas quando o relatório exigir registros obrigatoriamente relacionados.

                                    Regras sobre agregações:
                                    - Pode usar SUM, COUNT, AVG, MIN e MAX.
                                    - Pode usar COALESCE.
                                    - Pode usar STRING_AGG.
                                    - Pode usar CONCAT.
                                    - Pode usar TO_CHAR para formatar datas.
                                    - Pode usar ROW_NUMBER para ranking e últimas compras.
                                    - Pode usar CTE com WITH.
                                    - Pode usar subqueries.
                                    - Pode usar FILTER em agregações quando necessário.
                                    - Sempre use GROUP BY corretamente quando misturar campos comuns e agregações.
                                    - Evite gerar linhas duplicadas por joins com tabelas de itens ou estornos.
                                    - Quando juntar pedidos com itens e estornos, agregue itens e estornos em CTEs separadas antes do SELECT final.

                                    Regras sobre aliases:
                                    - Retorne aliases amigáveis em português.
                                    - Exemplos bons de aliases:
                                      cliente_id
                                      cliente_nome
                                      telefone
                                      celular
                                      email
                                      ultima_compra_data
                                      itens_ultima_compra
                                      itens_segunda_ultima_compra
                                      total_faturamento
                                      total_faturamento_liquido
                                    - Não use aliases confusos.
                                    - Não retorne nomes técnicos desnecessários quando puder usar aliases claros.

                                    Regras sobre campos de contato:
                                    - Só retorne telefone, celular ou e-mail quando o usuário pedir explicitamente.
                                    - Quando o usuário pedir telefone, retorne c.telefone se existir no schema.
                                    - Quando o usuário pedir celular, retorne c.celular se existir no schema.
                                    - Quando o usuário pedir e-mail ou email, retorne c.email se existir no schema.
                                    - Quando o usuário pedir contato ou dados de contato, pode retornar telefone, celular e e-mail, se existirem no schema.
                                    - Quando o usuário não pedir contato, não retorne c.telefone, c.celular nem c.email.

                                    Regras sobre colunas deletado_em:
                                    - Quando uma tabela tiver deletado_em, filtre registros ativos usando deletado_em IS NULL.
                                    - Para clientes, use c.deletado_em IS NULL quando existir.
                                    - Para pedidos, use p.deletado_em IS NULL quando existir.
                                    - Para pedido_itens, use pi.deletado_em IS NULL quando existir.
                                    - Para outras tabelas, aplique a mesma regra se a coluna existir no schema.

                                    Regras de qualidade:
                                    - Gere uma consulta que realmente responda ao pedido do usuário.
                                    - Se o usuário pedir dados detalhados, retorne colunas detalhadas.
                                    - Se o usuário pedir resumo, use agregações.
                                    - Se o usuário pedir ranking, use ORDER BY adequado.
                                    - Se o usuário pedir maiores valores, ordene do maior para o menor.
                                    - Se o usuário pedir menores valores, ordene do menor para o maior.
                                    - Se o usuário pedir clientes sem compra ou inativos, ordene por última compra, colocando clientes sem compra primeiro quando fizer sentido.
                                    - Evite consultas desnecessariamente complexas.
                                    - Prefira CTEs legíveis para relatórios complexos.

                                    Exemplo de regra para clientes inativos:
                                    - Para "clientes que não compraram faz 5 dias", calcule a última compra com MAX(COALESCE(p.pago_em, p.criado_em)).
                                    - Filtre clientes cuja última compra seja nula ou menor/igual a now() - INTERVAL '5 days'.
                                    - Não use p.pago_em IS NOT NULL nesse caso, pois pedidos sem pago_em podem representar compras criadas ainda válidas no relatório.

                                    Exemplo de regra para itens das últimas compras:
                                    - Primeiro selecione pedidos válidos.
                                    - Depois agregue itens por pedido usando pedido_itens.
                                    - Depois ranqueie os pedidos por cliente.
                                    - Depois selecione as últimas N compras.
                                    - Depois agregue ou projete os itens no SELECT final.

                                    Formato padrão para relatórios de vendas/pedidos:
                                    - Se o usuário pedir vendas, pedidos, faturamento, valores, maiores vendas, últimas vendas ou relatório de vendas sem especificar campos adicionais, retorne somente:
                                      pedido_id,
                                      cliente_nome,
                                      total_bruto,
                                      valor_estornado,
                                      total_liquido.
                                    - Não inclua itens por padrão.
                                    - Não inclua telefone por padrão.
                                    - Não inclua celular por padrão.
                                    - Não inclua e-mail por padrão.
                                    - Não inclua todas as colunas da tabela.
                                    - Nunca use SELECT *.

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