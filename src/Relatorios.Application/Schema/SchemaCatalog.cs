namespace Relatorios.Application.Schema;

public sealed class SchemaCatalog
{
    public List<SchemaTableDefinition> Tables { get; set; } = new();
    public List<string> BusinessRules { get; set; } = new();
}