using FluentAssertions;
using Relatorios.Application.DynamicReports;
using Relatorios.Domain.DynamicQuerying;

namespace Relatorios.Tests.DynamicReports;

public sealed class DynamicReportBusinessRulesApplierTests
{
    private readonly DynamicReportBusinessRulesApplier _applier = new(new DynamicDateRangeExtractor());

    [Fact]
    public void Apply_ShouldUsePagoEm_WhenPromptMentionsPaidSales()
    {
        // Arrange
        var plan = CreateBasePlan();

        // Act
        _applier.Apply(plan, "vendas pagas em março de 2026");

        // Assert
        plan.Filters.Should().Contain(x => x.Field == "p.pago_em" && x.Operator == ">=");
        plan.Filters.Should().Contain(x => x.Field == "p.pago_em" && x.Operator == "<");
    }

    [Fact]
    public void Apply_ShouldUseCanceladoEm_WhenPromptMentionsCanceledSales()
    {
        // Arrange
        var plan = CreateBasePlan();

        // Act
        _applier.Apply(plan, "vendas canceladas em março de 2026");

        // Assert
        plan.Filters.Should().Contain(x => x.Field == "p.cancelado_em" && x.Operator == ">=");
        plan.Filters.Should().Contain(x => x.Field == "p.cancelado_em" && x.Operator == "<");
    }

    [Fact]
    public void Apply_ShouldUseAtualizadoEm_WhenPromptMentionsUpdatedSales()
    {
        // Arrange
        var plan = CreateBasePlan();

        // Act
        _applier.Apply(plan, "vendas atualizadas hoje");

        // Assert
        plan.Filters.Should().Contain(x => x.Field == "p.atualizado_em" && x.Operator == ">=");
        plan.Filters.Should().Contain(x => x.Field == "p.atualizado_em" && x.Operator == "<");
    }

    [Fact]
    public void Apply_ShouldUseCriadoEm_WhenPromptMentionsCreatedSales()
    {
        // Arrange
        var plan = CreateBasePlan();

        // Act
        _applier.Apply(plan, "vendas criadas em março de 2026");

        // Assert
        plan.Filters.Should().Contain(x => x.Field == "p.criado_em" && x.Operator == ">=");
        plan.Filters.Should().Contain(x => x.Field == "p.criado_em" && x.Operator == "<");
    }

    [Fact]
    public void Apply_ShouldAddValueColumns_WhenPromptMentionsValorLiquido()
    {
        // Arrange
        var plan = CreateBasePlan();

        // Act
        _applier.Apply(plan, "vendas com valor líquido");

        // Assert
        plan.SelectFields.Should().Contain(x => x.Field == "p.id" && x.Alias == "id_venda");
        plan.SelectFields.Should().Contain(x => x.Field == "c.nome" && x.Alias == "cliente");
        plan.SelectFields.Should().Contain(x => x.Field == "p.total" && x.Alias == "valor_bruto");
        plan.SelectFields.Should().Contain(x => x.Alias == "valor_estornado");
        plan.SelectFields.Should().Contain(x => x.Alias == "valor_liquido");

        plan.Joins.Should().Contain(x => x.Table.Contains("pedido_estornos_parciais"));
    }

    [Fact]
    public void Apply_ShouldAddNsu_WhenPromptMentionsNsu()
    {
        // Arrange
        var plan = CreateBasePlan();

        // Act
        _applier.Apply(plan, "vendas com nsu e valor líquido");

        // Assert
        plan.SelectFields.Should().Contain(x => x.Field == "pt.nsu" && x.Alias == "nsu");
        plan.Joins.Should().Contain(x => x.Table == "pagamentos_transacoes" && x.Alias == "pt");
    }

    [Fact]
    public void Apply_ShouldAddAverageColumns_WhenPromptMentionsMedia()
    {
        // Arrange
        var plan = CreateBasePlan();

        // Act
        _applier.Apply(plan, "média geral de vendas");

        // Assert
        plan.SelectFields.Should().Contain(x => x.Field == "p.total" && x.Alias == "media_bruto" && x.Aggregation == "AVG");
        plan.SelectFields.Should().Contain(x => x.Alias == "media_estorno" && x.Aggregation == "AVG");
        plan.SelectFields.Should().Contain(x => x.Alias == "media_liquido" && x.Aggregation == "AVG");
    }

    private static DynamicQueryPlanDto CreateBasePlan()
    {
        return new DynamicQueryPlanDto
        {
            Source = "pedidos",
            SourceAlias = "p",
            SelectFields =
            [
                new DynamicQuerySelectFieldDto
                {
                    Field = "p.id",
                    Alias = "id",
                    Aggregation = string.Empty
                }
            ]
        };
    }
}