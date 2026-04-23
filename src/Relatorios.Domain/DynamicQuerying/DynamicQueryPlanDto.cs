namespace Relatorios.Domain.DynamicQuerying;

public sealed class DynamicQueryPlanDto
{
    public string Source { get; set; } = string.Empty;
    public string? SourceAlias { get; set; }

    public List<DynamicQueryJoinDto> Joins { get; set; } = new();
    public List<DynamicQuerySelectFieldDto> SelectFields { get; set; } = new();
    public List<DynamicQueryFilterDto> Filters { get; set; } = new();
    public List<string> GroupBy { get; set; } = new();
    public List<DynamicQueryOrderByDto> OrderBy { get; set; } = new();

    public int? Limit { get; set; }
}