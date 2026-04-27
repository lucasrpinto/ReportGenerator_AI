using Microsoft.Extensions.Options;
using Npgsql;
using Relatorios.Application.Schema;
using Relatorios.Infrastructure.Options;

namespace Relatorios.Infrastructure.Schema;

public sealed class DatabaseSchemaCatalogProvider : ISchemaCatalogProvider
{
    private readonly PostgresOptions _postgresOptions;
    private readonly SchemaCatalogOptions _catalogOptions;

    public DatabaseSchemaCatalogProvider(
        IOptions<PostgresOptions> postgresOptions,
        IOptions<SchemaCatalogOptions> catalogOptions)
    {
        _postgresOptions = postgresOptions.Value;
        _catalogOptions = catalogOptions.Value;
    }

    public SchemaCatalog GetCatalog()
    {
        var catalog = new SchemaCatalog
        {
            Tables = LoadTables(),
            BusinessRules = GetBusinessRules()
        };

        ApplyKnownAliases(catalog);
        ApplyAllowedJoins(catalog);

        return catalog;
    }

    private List<SchemaTableDefinition> LoadTables()
    {
        var allowedTables = _catalogOptions.AllowedTables
            .Select(x => x.Trim())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var result = new List<SchemaTableDefinition>();

        using var connection = new NpgsqlConnection(_postgresOptions.ConnectionString);
        connection.Open();

        const string sql = """
            SELECT
                table_name,
                column_name,
                data_type
            FROM information_schema.columns
            WHERE table_schema = 'public'
            ORDER BY table_name, ordinal_position;
            """;

        using var command = new NpgsqlCommand(sql, connection);
        using var reader = command.ExecuteReader();

        while (reader.Read())
        {
            var tableName = reader.GetString(reader.GetOrdinal("table_name"));
            var columnName = reader.GetString(reader.GetOrdinal("column_name"));
            var dataType = reader.GetString(reader.GetOrdinal("data_type"));

            if (!allowedTables.Contains(tableName))
            {
                continue;
            }

            var table = result.FirstOrDefault(x =>
                string.Equals(x.Name, tableName, StringComparison.OrdinalIgnoreCase));

            if (table is null)
            {
                table = new SchemaTableDefinition
                {
                    Name = tableName,
                    Alias = GetAliasForTable(tableName),
                    Description = $"Tabela {tableName}"
                };

                result.Add(table);
            }

            table.Columns.Add(new SchemaColumnDefinition
            {
                Name = columnName,
                Description = $"Tipo PostgreSQL: {dataType}"
            });
        }

        return result;
    }

    private static void ApplyKnownAliases(SchemaCatalog catalog)
    {
        foreach (var table in catalog.Tables)
        {
            table.Alias = GetAliasForTable(table.Name);
        }
    }

    private static void ApplyAllowedJoins(SchemaCatalog catalog)
    {
        var pedidos = catalog.Tables.FirstOrDefault(x =>
            string.Equals(x.Name, "pedidos", StringComparison.OrdinalIgnoreCase));

        if (pedidos is null)
        {
            return;
        }

        if (catalog.Tables.Any(x => string.Equals(x.Name, "clientes", StringComparison.OrdinalIgnoreCase)))
        {
            pedidos.AllowedJoins.Add(new SchemaJoinDefinition
            {
                Type = "INNER JOIN",
                Table = "clientes",
                Alias = "c",
                On = "c.id = p.id_cliente"
            });
        }

        if (catalog.Tables.Any(x => string.Equals(x.Name, "pagamentos_transacoes", StringComparison.OrdinalIgnoreCase)))
        {
            pedidos.AllowedJoins.Add(new SchemaJoinDefinition
            {
                Type = "LEFT JOIN",
                Table = "pagamentos_transacoes",
                Alias = "pt",
                On = "pt.id_pedido = p.id"
            });
        }

        if (catalog.Tables.Any(x => string.Equals(x.Name, "pedido_estornos_parciais", StringComparison.OrdinalIgnoreCase)))
        {
            pedidos.AllowedJoins.Add(new SchemaJoinDefinition
            {
                Type = "LEFT JOIN",
                Table = "pedido_estornos_parciais",
                Alias = "pep",
                On = "pep.id_pedido = p.id"
            });
        }
    }

    private static string GetAliasForTable(string tableName)
    {
        return tableName.ToLowerInvariant() switch
        {
            "pedidos" => "p",
            "clientes" => "c",
            "pagamentos_transacoes" => "pt",
            "pedido_estornos_parciais" => "pep",
            _ => tableName[..1].ToLowerInvariant()
        };
    }

    private static List<string> GetBusinessRules()
    {
        return
        [
            "pedido pago = p.pago_em IS NOT NULL",
            "pedido sem pagamento = p.pago_em IS NULL",
            "pedido cancelado = p.cancelado_em IS NOT NULL",
            "pedido ativo = p.deletado_em IS NULL",
            "valor líquido = p.total - valor estornado",
            "use somente tabelas, colunas e joins informados",
            "nunca gere INSERT, UPDATE, DELETE, DROP ou ALTER",
            "as agregações permitidas são SUM, COUNT e AVG"
        ];
    }
}