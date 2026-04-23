namespace Relatorios.Infrastructure.Options;

public sealed class ReportStorageOptions
{
    public const string SectionName = "ReportStorage";

    public string BasePath { get; set; } = "GeneratedReports";
}