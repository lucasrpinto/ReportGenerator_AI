using Relatorios.Application.Schema;

namespace Relatorios.Infrastructure.Schema;

public sealed class StaticSchemaCatalogProvider : ISchemaCatalogProvider
{
    public SchemaCatalog GetCatalog()
    {
        return new SchemaCatalog
        {
            Tables =
            [
                new SchemaTableDefinition
                {
                    Name = "pedidos",
                    Alias = "p",
                    Description = "Tabela principal de pedidos",
                    Columns =
                    [
                        new SchemaColumnDefinition { Name = "id", Description = "Identificador do pedido" },
                        new SchemaColumnDefinition { Name = "id_cliente", Description = "Identificador do cliente" },
                        new SchemaColumnDefinition { Name = "total", Description = "Valor total do pedido" },
                        new SchemaColumnDefinition { Name = "pago_em", Description = "Data de pagamento" },
                        new SchemaColumnDefinition { Name = "cancelado_em", Description = "Data de cancelamento" },
                        new SchemaColumnDefinition { Name = "reembolsado_em", Description = "Data de reembolso" },
                        new SchemaColumnDefinition { Name = "deletado_em", Description = "Data de exclusão lógica" }
                    ],
                    AllowedJoins =
                    [
                        new SchemaJoinDefinition
                        {
                            Type = "INNER JOIN",
                            Table = "clientes",
                            Alias = "c",
                            On = "c.id = p.id_cliente"
                        },
                        new SchemaJoinDefinition
                        {
                            Type = "LEFT JOIN",
                            Table = "pagamentos_transacoes",
                            Alias = "pt",
                            On = "pt.id_pedido = p.id"
                        }
                    ]
                },
                new SchemaTableDefinition
                {
                    Name = "clientes",
                    Alias = "c",
                    Description = "Tabela de clientes",
                    Columns =
                    [
                        new SchemaColumnDefinition { Name = "id", Description = "Identificador do cliente" },
                        new SchemaColumnDefinition { Name = "nome", Description = "Nome do cliente" }
                    ]
                },
                new SchemaTableDefinition
                {
                    Name = "pagamentos_transacoes",
                    Alias = "pt",
                    Description = "Tabela de transações de pagamento",
                    Columns =
                    [
                        new SchemaColumnDefinition { Name = "id", Description = "Identificador da transação" },
                        new SchemaColumnDefinition { Name = "id_pedido", Description = "Pedido relacionado" },
                        new SchemaColumnDefinition { Name = "nsu", Description = "NSU da transação" }
                    ]
                },
                new SchemaTableDefinition
                {
                    Name = "pedido_estornos_parciais",
                    Alias = "pep",
                    Description = "Tabela de estornos parciais de pedido",
                    Columns =
                    [
                        new SchemaColumnDefinition { Name = "id_pedido", Description = "Pedido relacionado" },
                        new SchemaColumnDefinition { Name = "valor_estornado", Description = "Valor estornado" }
                    ]
                }
            ],
            BusinessRules =
            [
                "pedido pago = p.pago_em IS NOT NULL",
                "pedido sem pagamento = p.pago_em IS NULL",
                "pedido cancelado = p.cancelado_em IS NOT NULL",
                "pedido ativo = p.deletado_em IS NULL",
                "valor líquido = p.total - valor estornado",
                "use somente tabelas, colunas e joins informados",
                "nunca gere INSERT, UPDATE, DELETE, DROP ou ALTER"
            ]
        };
    }
}