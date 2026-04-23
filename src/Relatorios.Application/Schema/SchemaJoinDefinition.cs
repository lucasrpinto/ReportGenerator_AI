namespace Relatorios.Application.Schema;

public sealed class SchemaJoinDefinition
{
    public string Type { get; set; } = string.Empty;
    public string Table { get; set; } = string.Empty;
    public string Alias { get; set; } = string.Empty;
    public string On { get; set; } = string.Empty;
}