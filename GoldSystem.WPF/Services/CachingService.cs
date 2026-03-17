using GoldSystem.Core.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace GoldSystem.WPF.Services;

/// <summary>
/// In-process memory cache backed by <see cref="IMemoryCache"/>.
/// Tracks all active keys so the whole cache can be cleared atomically.
/// </summary>
public sealed class CachingService : ICachingService
{
    private readonly IMemoryCache              _memoryCache;
    private readonly ILogger<CachingService>   _logger;
    private readonly ConcurrentDictionary<string, byte> _keys = new();

    private static readonly TimeSpan DefaultTtl = TimeSpan.FromHours(1);

    public CachingService(IMemoryCache memoryCache, ILogger<CachingService> logger)
    {
        _memoryCache = memoryCache;
        _logger      = logger;
    }

    public Task<T?> GetAsync<T>(string key)
    {
        if (_memoryCache.TryGetValue(key, out T? value))
        {
            _logger.LogDebug("Cache hit  – {Key}", key);
            return Task.FromResult<T?>(value);
        }

        _logger.LogDebug("Cache miss – {Key}", key);
        return Task.FromResult<T?>(default);
    }

    public Task SetAsync<T>(string key, T value, TimeSpan? ttl = null)
    {
        var options = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = ttl ?? DefaultTtl
        };

        _memoryCache.Set(key, value, options);
        _keys.TryAdd(key, 0);
        _logger.LogDebug("Cache set  – {Key} (TTL {TTL}ms)",
            key, (ttl ?? DefaultTtl).TotalMilliseconds);

        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key)
    {
        _memoryCache.Remove(key);
        _keys.TryRemove(key, out _);
        _logger.LogDebug("Cache remove – {Key}", key);
        return Task.CompletedTask;
    }

    public Task ClearAsync()
    {
        foreach (var key in _keys.Keys)
            _memoryCache.Remove(key);

        _keys.Clear();
        _logger.LogInformation("Cache cleared");
        return Task.CompletedTask;
    }

    public async Task<T?> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan? ttl = null)
    {
        if (_memoryCache.TryGetValue(key, out T? value))
            return value;

        value = await factory();
        await SetAsync(key, value, ttl);
        return value;
    }
}
