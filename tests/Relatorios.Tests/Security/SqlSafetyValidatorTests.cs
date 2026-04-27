using FluentAssertions;
using Relatorios.Infrastructure.Security;

namespace Relatorios.Tests.Security;

public sealed class SqlSafetyValidatorTests
{
    private readonly SqlSafetyValidator _validator = new();

    [Fact]
    public void ValidateOrThrow_ShouldAllowSimpleSelect()
    {
        // Arrange
        const string sql = "SELECT p.id, p.total FROM pedidos p";

        // Act
        var act = () => _validator.ValidateOrThrow(sql);

        // Assert
        act.Should().NotThrow();
    }

    [Theory]
    [InlineData("DELETE FROM pedidos")]
    [InlineData("DROP TABLE pedidos")]
    [InlineData("UPDATE pedidos SET total = 0")]
    [InlineData("INSERT INTO pedidos (id) VALUES (1)")]
    [InlineData("ALTER TABLE pedidos ADD COLUMN teste text")]
    [InlineData("CREATE TABLE teste (id int)")]
    [InlineData("TRUNCATE TABLE pedidos")]
    [InlineData("EXEC alguma_funcao")]
    [InlineData("CALL alguma_funcao()")]
    public void ValidateOrThrow_ShouldBlockForbiddenCommands(string sql)
    {
        // Act
        var act = () => _validator.ValidateOrThrow(sql);

        // Assert
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void ValidateOrThrow_ShouldBlockMultipleCommands()
    {
        // Arrange
        const string sql = "SELECT * FROM pedidos; SELECT * FROM clientes";

        // Act
        var act = () => _validator.ValidateOrThrow(sql);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Múltiplos comandos*");
    }

    [Fact]
    public void ValidateOrThrow_ShouldBlockNonSelectSql()
    {
        // Arrange
        const string sql = "WITH teste AS (SELECT 1) SELECT * FROM teste";

        // Act
        var act = () => _validator.ValidateOrThrow(sql);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Somente consultas SELECT*");
    }

    [Fact]
    public void ValidateOrThrow_ShouldBlockEmptySql()
    {
        // Act
        var act = () => _validator.ValidateOrThrow("");

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*vazio*");
    }
}