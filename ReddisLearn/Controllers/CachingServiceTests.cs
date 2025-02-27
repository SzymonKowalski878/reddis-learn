using Microsoft.AspNetCore.Mvc;

namespace ReddisLearn.Controllers;

public class CachingServiceTests : ControllerBase
{
    private readonly ICachingService _cachingService;

    public CachingServiceTests(ICachingService cachingService)
    {
        _cachingService = cachingService;
    }

    [HttpGet("cache/{key}")]
    public IActionResult GetCachedValue(string key)
    {
        var result = _cachingService.GetOrSet(key, MockFetchFunction, TimeSpan.FromSeconds(2));
        return Ok(result);
    }

    [HttpGet("cache-async/{key}")]
    public async Task<IActionResult> GetCachedValueAsync(string key)
    {
        var result = await _cachingService.GetOrSetAsync(key, MockFetchFunctionAsync, TimeSpan.FromSeconds(2));
        return Ok(result);
    }

    private DateTimeOffset MockFetchFunction()
    {
        // Simulating a slow fetch
        Task.Delay(5000).Wait(); // Simulates delay
        return DateTimeOffset.UtcNow;
    }

    private async Task<DateTimeOffset> MockFetchFunctionAsync()
    {
        // Simulating a slow fetch
        await Task.Delay(5000); // Simulates delay
        return DateTimeOffset.UtcNow;
    }
}
