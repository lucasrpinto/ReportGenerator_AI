namespace Relatorios.Domain.DynamicQuerying;

public sealed class DynamicQueryOrderByDto
{
    public string Field { get; set; } = string.Empty;
    public string Direction { get; set; } = "ASC";
}