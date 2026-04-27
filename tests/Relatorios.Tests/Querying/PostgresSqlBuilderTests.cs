using FluentAssertions;
using Relatorios.Domain.Querying;
using Relatorios.Infrastructure.Persistence.QueryExecution;

namespace Relatorios.Tests.Querying;

public sealed class PostgresSqlBuilderTests
{
    [Fact]
    public void Build_ShouldCreateSimpleSelect()
    {
        // Arrange
        var plan = new QueryPlan
        {
            Source = "pedidos",
            SourceAlias = "p",
            SelectFields =
            [
                new QuerySelectField
                {
                    Field = "p.id",
                    Alias = "id_venda"
                },
                new QuerySelectField
                {
                    Field = "p.total",
                    Alias = "valor_total"
                }
            ]
        };

        // Act
        var result = PostgresSqlBuilder.Build(plan);

        // Assert
        result.Sql.Should().Contain("SELECT p.id AS \"id_venda\", p.total AS \"valor_total\"");
        result.Sql.Should().Contain("FROM pedidos p");
        result.Parameters.Should().BeEmpty();
    }

    [Fact]
    public void Build_ShouldCreateWhereWithParameters()
    {
        // Arrange
        var startDate = new DateTime(2026, 3, 1);
        var endDate = new DateTime(2026, 4, 1);

        var plan = new QueryPlan
        {
            Source = "pedidos",
            SourceAlias = "p",
            SelectFields =
            [
                new QuerySelectField
                {
                    Field = "p.id",
                    Alias = "id_venda"
                }
            ],
            Filters =
            [
                new QueryFilter
                {
                    Field = "p.criado_em",
                    Operator = ">=",
                    Value = startDate
                },
                new QueryFilter
                {
                    Field = "p.criado_em",
                    Operator = "<",
                    Value = endDate
                }
            ]
        };

        // Act
        var result = PostgresSqlBuilder.Build(plan);

        // Assert
        result.Sql.Should().Contain("WHERE p.criado_em >= @p0 AND p.criado_em < @p1");
        result.Parameters.Should().HaveCount(2);
        result.Parameters[0].Value.Should().Be(startDate);
        result.Parameters[1].Value.Should().Be(endDate);
    }

    [Fact]
    public void Build_ShouldCreateJoin()
    {
        // Arrange
        var plan = new QueryPlan
        {
            Source = "pedidos",
            SourceAlias = "p",
            SelectFields =
            [
                new QuerySelectField
                {
                    Field = "p.id",
                    Alias = "id_venda"
                },
                new QuerySelectField
                {
                    Field = "c.nome",
                    Alias = "cliente"
                }
            ],
            Joins =
            [
                new QueryJoin
                {
                    Type = "INNER JOIN",
                    Table = "clientes",
                    Alias = "c",
                    On = "c.id = p.id_cliente"
                }
            ]
        };

        // Act
        var result = PostgresSqlBuilder.Build(plan);

        // Assert
        result.Sql.Should().Contain("INNER JOIN clientes c ON c.id = p.id_cliente");
    }

    [Fact]
    public void Build_ShouldCreateOrderByAndLimit()
    {
        // Arrange
        var plan = new QueryPlan
        {
            Source = "pedidos",
            SourceAlias = "p",
            SelectFields =
            [
                new QuerySelectField
                {
                    Field = "p.id",
                    Alias = "id_venda"
                },
                new QuerySelectField
                {
                    Field = "p.total",
                    Alias = "valor_total"
                }
            ],
            OrderByFields =
            [
                new QueryOrderBy
                {
                    Field = "p.total",
                    Direction = "DESC"
                }
            ],
            Limit = 10
        };

        // Act
        var result = PostgresSqlBuilder.Build(plan);

        // Assert
        result.Sql.Should().Contain("ORDER BY p.total DESC");
        result.Sql.Should().Contain("LIMIT @limit");
        result.Parameters.Should().ContainSingle(x => x.ParameterName == "@limit");
        result.Parameters.Single(x => x.ParameterName == "@limit").Value.Should().Be(10);
    }

    [Fact]
    public void Build_ShouldThrow_WhenSourceIsEmpty()
    {
        // Arrange
        var plan = new QueryPlan
        {
            SelectFields =
            [
                new QuerySelectField
                {
                    Field = "p.id"
                }
            ]
        };

        // Act
        var act = () => PostgresSqlBuilder.Build(plan);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*fonte de dados*");
    }

    [Fact]
    public void Build_ShouldThrow_WhenSelectFieldsIsEmpty()
    {
        // Arrange
        var plan = new QueryPlan
        {
            Source = "pedidos",
            SourceAlias = "p"
        };

        // Act
        var act = () => PostgresSqlBuilder.Build(plan);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*campos de seleção*");
    }
}