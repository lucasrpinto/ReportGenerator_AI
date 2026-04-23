namespace Relatorios.Domain.Reporting;

public sealed class SemanticExample
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string ExampleText { get; set; } = string.Empty;
    public string IntentName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Limit { get; set; } = 10;
    public string[] Tags { get; set; } = [];
}