using StackExchange.Redis;
using TourneyMate.Redis.Config;

namespace TourneyMate.Redis.Infrastructure;

public sealed class RedisContext : IAsyncDisposable
{
    private readonly ConnectionMultiplexer _mux;
    private readonly int _db;

    public RedisContext(RedisConfig cfg)
    {
        cfg.Validate();
        _db = cfg.Database;
        _mux = ConnectionMultiplexer.Connect(cfg.ConnectionString);
    }

    public IDatabase Db => _mux.GetDatabase(_db);

    public async ValueTask DisposeAsync()
    {
        await _mux.CloseAsync();
        _mux.Dispose();
    }
}
