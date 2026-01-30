using TourneyMate.Redis.Infrastructure;

namespace TourneyMate.Redis.Repositories;

public sealed class RateLimiterRepository
{
    private readonly RedisContext _ctx;
    public RateLimiterRepository(RedisContext ctx) => _ctx = ctx;

    private static string Key(string scope, string id) => $"tm:rl:{scope}:{id}";

    // npr. max 5 zahteva u 10s
    public async Task<bool> AllowAsync(string scope, string id, int max, TimeSpan window)
    {
        var key = Key(scope, id);
        var count = await _ctx.Db.StringIncrementAsync(key);

        if (count == 1)
            await _ctx.Db.KeyExpireAsync(key, window);

        return count <= max;
    }
}
