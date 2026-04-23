namespace Relatorios.Infrastructure.Options;

public sealed class EmbeddingOptions
{
    public const string SectionName = "Embeddings";

    public string Provider { get; set; } = "HuggingFace";
    public string Model { get; set; } = "intfloat/multilingual-e5-large-instruct";
    public string ApiKey { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://api-inference.huggingface.co/models";
}