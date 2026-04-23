namespace Relatorios.Application.UseCases.Reports.PreviewReport;

// Comando da pré-visualização
public sealed class PreviewReportCommand
{
    public string Prompt { get; set; } = string.Empty;
}