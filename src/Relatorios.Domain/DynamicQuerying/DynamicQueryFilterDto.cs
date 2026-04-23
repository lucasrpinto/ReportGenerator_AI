namespace Relatorios.Domain.DynamicQuerying;

public sealed class DynamicQueryFilterDto
{
    public string Field { get; set; } = string.Empty;
    public string Operator { get; set; } = string.Empty;
    public object? Value { get; set; }
}