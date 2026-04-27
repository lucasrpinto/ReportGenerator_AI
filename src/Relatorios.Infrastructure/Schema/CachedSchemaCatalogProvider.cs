using Microsoft.Extensions.Caching.Memory;
using Relatorios.Application.Schema;

namespace Relatorios.Infrastructure.Schema;

public sealed class CachedSchemaCatalogProvider : ISchemaCatalogProvider
{
    private const string CacheKey = "schema_catalog";
    private readonly IMemoryCache _cache;
    private readonly DatabaseSchemaCatalogProvider _innerProvider;

    public CachedSchemaCatalogProvider(
        IMemoryCache cache,
        DatabaseSchemaCatalogProvider innerProvider)
    {
        _cache = cache;
        _innerProvider = innerProvider;
    }

    public SchemaCatalog GetCatalog()
    {
        return _cache.GetOrCreate(CacheKey, entry =>
        {
            // Cache simples para evitar leitura constante do schema.
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10);

            return _innerProvider.GetCatalog();
        })!;
    }
}