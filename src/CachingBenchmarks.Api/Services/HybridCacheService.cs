using Microsoft.Extensions.Caching.Hybrid;

namespace CachingBenchmarks.Api.Services;

public class HybridCacheService : ICacheService
{
    private readonly HybridCache _cache;

    public HybridCacheService(HybridCache cache)
    {
        _cache = cache;
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _cache.GetOrCreateAsync<T>(
                key,
                _ => default(ValueTask<T>),
                cancellationToken: cancellationToken
            );
        }
        catch
        {
            return default;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
    {
        var options = new HybridCacheEntryOptions();

        if (expiration.HasValue)
        {
            options.Expiration = expiration.Value;
        }

        await _cache.SetAsync(key, value, options, cancellationToken: cancellationToken);
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        await _cache.RemoveAsync(key, cancellationToken);
    }
}
