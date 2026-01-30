using System.Text.Json;
using StackExchange.Redis;
using TourneyMate.Redis.Infrastructure;

namespace TourneyMate.Redis.Repositories;

public sealed record SessionUser(string Username, string DisplayName, string Role);

public sealed class SessionRepository
{
    private readonly IDatabase _db;
    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

    public SessionRepository(RedisContext ctx)
    {
        _db = ctx.Db;
    }

    private static string Key(string token) => $"tm:sess:{token}";

    public async Task SetAsync(string token, SessionUser user, TimeSpan ttl)
    {
        var json = JsonSerializer.Serialize(user, JsonOpts);
        await _db.StringSetAsync(Key(token), json, ttl);
    }

    public async Task<SessionUser?> GetAsync(string token)
    {
        var v = await _db.StringGetAsync(Key(token));
        if (!v.HasValue) return null;

        var json = v.ToString();
        return JsonSerializer.Deserialize<SessionUser>(json, JsonOpts);
    }


    public Task<bool> DeleteAsync(string token) => _db.KeyDeleteAsync(Key(token));
}
