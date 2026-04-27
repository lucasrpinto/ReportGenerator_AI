using FluentAssertions;
using Relatorios.Application.DynamicReports;

namespace Relatorios.Tests.DynamicReports;

public sealed class DynamicDateRangeExtractorTests
{
    [Fact]
    public void Extract_ShouldReturnTodayRange_WhenPromptContainsHoje()
    {
        // Arrange
        var extractor = new DynamicDateRangeExtractor();
        var today = DateTime.Today;

        // Act
        var result = extractor.Extract("vendas de hoje");

        // Assert
        result.Should().NotBeNull();
        result!.Start.Should().Be(today);
        result.EndExclusive.Should().Be(today.AddDays(1));
    }

    [Fact]
    public void Extract_ShouldReturnCurrentMonthRange_WhenPromptContainsEsteMes()
    {
        // Arrange
        var extractor = new DynamicDateRangeExtractor();
        var today = DateTime.Today;
        var expectedStart = new DateTime(today.Year, today.Month, 1);

        // Act
        var result = extractor.Extract("vendas deste mês");

        // Assert
        result.Should().NotBeNull();
        result!.Start.Should().Be(expectedStart);
        result.EndExclusive.Should().Be(expectedStart.AddMonths(1));
    }

    [Fact]
    public void Extract_ShouldReturnSpecificMonthRange_WhenPromptContainsMonthAndYear()
    {
        // Arrange
        var extractor = new DynamicDateRangeExtractor();

        // Act
        var result = extractor.Extract("vendas de março de 2026");

        // Assert
        result.Should().NotBeNull();
        result!.Start.Should().Be(new DateTime(2026, 3, 1));
        result.EndExclusive.Should().Be(new DateTime(2026, 4, 1));
    }

    [Fact]
    public void Extract_ShouldReturnExplicitRange_WhenPromptContainsDateRange()
    {
        // Arrange
        var extractor = new DynamicDateRangeExtractor();

        // Act
        var result = extractor.Extract("vendas entre 24/03/2026 até 24/04/2026");

        // Assert
        result.Should().NotBeNull();
        result!.Start.Should().Be(new DateTime(2026, 3, 24));
        result.EndExclusive.Should().Be(new DateTime(2026, 4, 25));
    }

    [Fact]
    public void Extract_ShouldReturnCurrentWeekRange_WhenPromptContainsEstaSemana()
    {
        // Arrange
        var extractor = new DynamicDateRangeExtractor();
        var today = DateTime.Today;

        var diff = (7 + (today.DayOfWeek - DayOfWeek.Monday)) % 7;
        var expectedStart = today.AddDays(-diff);

        // Act
        var result = extractor.Extract("vendas desta semana");

        // Assert
        result.Should().NotBeNull();
        result!.Start.Should().Be(expectedStart);
        result.EndExclusive.Should().Be(expectedStart.AddDays(7));
    }

    [Fact]
    public void Extract_ShouldReturnNull_WhenPromptDoesNotContainDateExpression()
    {
        // Arrange
        var extractor = new DynamicDateRangeExtractor();

        // Act
        var result = extractor.Extract("maiores vendas por cliente");

        // Assert
        result.Should().BeNull();
    }
}