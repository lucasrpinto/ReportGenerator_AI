namespace Relatorios.Domain.DynamicQuerying;

public sealed class DynamicQuerySelectFieldDto
{
    public string Field { get; set; } = string.Empty;
    public string? Alias { get; set; }
    public string? Aggregation { get; set; }
}