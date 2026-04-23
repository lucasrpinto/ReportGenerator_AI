namespace Relatorios.Application.Schema;

public sealed class SchemaTableDefinition
{
    public string Name { get; set; } = string.Empty;
    public string Alias { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public List<SchemaColumnDefinition> Columns { get; set; } = new();
    public List<SchemaJoinDefinition> AllowedJoins { get; set; } = new();
}