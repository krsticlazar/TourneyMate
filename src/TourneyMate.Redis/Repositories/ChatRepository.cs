using System.Text.Json;
using StackExchange.Redis;
using TourneyMate.Redis.Infrastructure;
using TourneyMate.Redis.Models;

namespace TourneyMate.Redis.Repositories;

public sealed class ChatRepository
{
    private readonly RedisContext _ctx;
    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

    public ChatRepository(RedisContext ctx) => _ctx = ctx;

    public async Task PushMessageAsync(string channelKey, ChatMessage msg, int keepLast = 200)
    {
        if (string.IsNullOrWhiteSpace(channelKey))
            throw new ArgumentException("channelKey is required.", nameof(channelKey));

        if (keepLast <= 0) keepLast = 200;
        if (keepLast > 2000) keepLast = 2000;

        var json = JsonSerializer.Serialize(msg, JsonOpts);

        // LPUSH (newest first) + LTRIM: zadrzi poslednjih N poruka
        var tran = _ctx.Db.CreateTransaction();
        _ = tran.ListLeftPushAsync(channelKey, json);
        _ = tran.ListTrimAsync(channelKey, 0, keepLast - 1);
        await tran.ExecuteAsync();
    }

    public async Task<IReadOnlyList<ChatMessage>> GetLastAsync(string channelKey, int count = 50)
    {
        if (string.IsNullOrWhiteSpace(channelKey))
            return Array.Empty<ChatMessage>();

        if (count <= 0) count = 50;
        if (count > 500) count = 500;

        // Sa LPUSH je newest-first u listi. Uzimamo prvih N (0..N-1), pa okrenemo da UI bude oldest-first.
        var items = await _ctx.Db.ListRangeAsync(channelKey, 0, count - 1);
        if (items.Length == 0) return Array.Empty<ChatMessage>();

        var list = new List<ChatMessage>(items.Length);

        foreach (var x in items)
        {
            if (!x.HasValue) continue;

            var json = x.ToString();
            if (string.IsNullOrWhiteSpace(json)) continue;

            try
            {
                var msg = JsonSerializer.Deserialize<ChatMessage>(json, JsonOpts);
                if (msg is not null) list.Add(msg);
            }
            catch
            {
                // ignore invalid entries
            }
        }

        list.Reverse(); // oldest-first za prikaz
        return list;
    }
}
