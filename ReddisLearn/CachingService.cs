using StackExchange.Redis;
using System.Text.Json;

namespace ReddisLearn;

public interface ICachingService
{
    T GetOrSet<T>(string key, Func<T> fetchFunction, TimeSpan cacheExpiry);
    Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> fetchFunction, TimeSpan cacheExpiry);
}

public class CachingService : ICachingService
{
    private readonly IDatabase _cache;
    private readonly IRedLockService _lockService;

    public CachingService(IConfiguration configuration, IRedLockService lockService)
    {
        var redisConnectionString = configuration.GetConnectionString("Redis");
        var connectionMultiplexer = ConnectionMultiplexer.Connect(redisConnectionString);
        _cache = connectionMultiplexer.GetDatabase();
        _lockService = lockService;
    }

    public static DateTimeOffset MockFetchFunction()
    {
        return DateTimeOffset.UtcNow;
    }

    public T GetOrSet<T>(string key, Func<T> fetchFunction, TimeSpan cacheExpiry)
    {
        var cachedValue = _cache.StringGet(key);
        if (!cachedValue.IsNullOrEmpty)
        {
            return JsonSerializer.Deserialize<T>(cachedValue);
        }

        using (var redLock = _lockService.AcquireLock(key))
        {
            if (redLock.IsAcquired)
            {
                var result = fetchFunction();
                _cache.StringSet(key, JsonSerializer.Serialize(result), cacheExpiry);
                return result;
            }
        }

        return default;
    }

    public async Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> fetchFunction, TimeSpan cacheExpiry)
    {
        var cachedValue = await _cache.StringGetAsync(key);
        if (!cachedValue.IsNullOrEmpty)
        {
            return JsonSerializer.Deserialize<T>(cachedValue);
        }

        await using (var redLock = await _lockService.AcquireLockAsync(key))
        {
            if (redLock.IsAcquired)
            {
                var result = await fetchFunction();
                await _cache.StringSetAsync(key, JsonSerializer.Serialize(result), cacheExpiry);
                return result;
            }
        }

        return default;
    }
}