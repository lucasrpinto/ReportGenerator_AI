namespace Relatorios.Domain.DynamicQuerying;

public sealed class DynamicQueryJoinDto
{
    public string Type { get; set; } = string.Empty;
    public string Table { get; set; } = string.Empty;
    public string? Alias { get; set; }
    public string On { get; set; } = string.Empty;
}