namespace Relatorios.Contracts.Requests;

public sealed class PreviewDynamicReportRequest
{
    public string Prompt { get; set; } = string.Empty;

    // Página do preview. Começa em 1.
    public int? Page { get; set; }

    // Tamanho da página. Máximo permitido: 25.
    public int? PageSize { get; set; }
}