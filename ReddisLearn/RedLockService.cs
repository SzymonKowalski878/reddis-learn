using RedLockNet;
using RedLockNet.SERedis;
using StackExchange.Redis;

namespace ReddisLearn;

public interface IRedLockService
{
    IRedLock AcquireLock(string key);
    Task<IRedLock> AcquireLockAsync(string key);
}

public class RedLockService : IRedLockService
{
    private readonly IDistributedLockFactory _redLockFactory;
    private readonly TimeSpan _expiry;
    private readonly int _maxRetries;
    private readonly TimeSpan _retryDelay;


    public RedLockService(IConfiguration configuration, ILoggerFactory loggerFactory)
    {
        var redisConnectionString = configuration.GetConnectionString("Redis");
        var connection = ConnectionMultiplexer.Connect(redisConnectionString!);
        _redLockFactory = RedLockFactory.Create([connection], loggerFactory);

        int expirySeconds = configuration.GetValue<int>("LockExpirySeconds", 15);
        _expiry = TimeSpan.FromSeconds(expirySeconds > 0 ? expirySeconds : 15);

        _maxRetries = configuration.GetValue<int>("MaxLockRetries", 5);
        _retryDelay = TimeSpan.FromMilliseconds(configuration.GetValue<int>("LockRetryDelayMs", 100));
    }

    public IRedLock AcquireLock(string key)
    {
        return AcquireLockWithRetry(key);
    }

    public async Task<IRedLock> AcquireLockAsync(string key)
    {
        return await AcquireLockWithRetryAsync(key);
    }

    private IRedLock AcquireLockWithRetry(string key)
    {
        IRedLock redLock = null;

        for (int attempt = 0; attempt < _maxRetries; attempt++)
        {
            redLock = _redLockFactory.CreateLock(key, _expiry);
            if (redLock.IsAcquired)
            {
                return redLock;
            }

            // Wait before retrying
            Task.Delay(_retryDelay).Wait();
        }

        return redLock;
    }

    private async Task<IRedLock> AcquireLockWithRetryAsync(string key)
    {
        IRedLock redLock = null;

        for (int attempt = 0; attempt < _maxRetries; attempt++)
        {
            redLock = await _redLockFactory.CreateLockAsync(key, _expiry);
            if (redLock.IsAcquired)
            {
                return redLock;
            }

            // Wait before retrying
            await Task.Delay(_retryDelay);
        }

        return redLock; // Return the lock, which will be null if not acquired
    }
}