namespace Relatorios.Contracts.Requests;

// Requisição para pré-visualizar o relatório na tela
public sealed class PreviewReportRequest
{
    public string Prompt { get; set; } = string.Empty;
}