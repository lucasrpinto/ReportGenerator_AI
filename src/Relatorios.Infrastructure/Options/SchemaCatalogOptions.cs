namespace Relatorios.Infrastructure.Options;

public sealed class SchemaCatalogOptions
{
    public const string SectionName = "SchemaCatalog";

    public List<string> AllowedTables { get; set; } = new();
}