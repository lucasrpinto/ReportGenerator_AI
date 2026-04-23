namespace Relatorios.Infrastructure.Options;

// Opções da conexão do banco secundário
public sealed class ReportHistoryDatabaseOptions
{
    public const string SectionName = "ReportHistoryDatabase";

    public string ConnectionString { get; set; } = string.Empty;
}