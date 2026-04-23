namespace Relatorios.Domain.Reporting;

public sealed class SemanticSearchResult
{
    public SemanticExample Example { get; set; } = default!;
    public double Score { get; set; }
}