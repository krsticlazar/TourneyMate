using TourneyMate.Redis.Repositories;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using TourneyMate.Redis.Infrastructure;

namespace TourneyMate.Api.Services;

/// <summary>
/// Background service za čišćenje starih podataka iz Redis-a pri application shutdown (CTRL+C)
/// </summary>
public sealed class CleanupService : IHostedService
{
    private readonly RedisContext _redis;
    private readonly ILogger<CleanupService> _logger;

    public CleanupService(RedisContext redis, ILogger<CleanupService> logger)
    {
        _redis = redis;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("CleanupService started - will run cleanup tasks on shutdown.");
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("🧹 CleanupService stopping - running cleanup tasks...");

        try
        {
            var db = _redis.Db;

            // 1. Obriši stare GLOBAL CHAT poruke (starije od 30 dana)
            await CleanupOldChatMessages(db, "chat:global", 30);

            // 2. Obriši stare TOURNAMENT CHAT poruke (starije od 30 dana)
            await CleanupAllTournamentChats(db, 30);

            // 3. Obriši SVE SESIJE (opciono - sesije ionako imaju TTL 1h)
            // Sessions već imaju TTL, ali možemo eksplicitno da očistimo
            await CleanupExpiredSessions(db);

            _logger.LogInformation("✅ Cleanup completed successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error during cleanup.");
        }
    }

    private async Task CleanupOldChatMessages(IDatabase db, string chatKey, int daysOld)
    {
        _logger.LogInformation("Cleaning up {ChatKey} messages older than {Days} days...", chatKey, daysOld);

        var cutoffDate = DateTimeOffset.UtcNow.AddDays(-daysOld);
        var messages = await db.ListRangeAsync(chatKey);
        
        int deletedCount = 0;

        foreach (var message in messages)
        {
            try
            {
                var json = System.Text.Json.JsonDocument.Parse(message.ToString());
                if (json.RootElement.TryGetProperty("timestampUtc", out var timestampProp))
                {
                    var timestamp = DateTimeOffset.Parse(timestampProp.GetString()!);
                    if (timestamp < cutoffDate)
                    {
                        await db.ListRemoveAsync(chatKey, message);
                        deletedCount++;
                    }
                }
            }
            catch
            {
                // Ignoriši loše formatirane poruke
            }
        }

        _logger.LogInformation("Deleted {Count} old messages from {ChatKey}", deletedCount, chatKey);
    }

    private async Task CleanupAllTournamentChats(IDatabase db, int daysOld)
    {
        _logger.LogInformation("Cleaning up all tournament chat messages older than {Days} days...", daysOld);

        // Pronađi sve tournament chat key-eve (chat:tournament:*)
        var server = _redis.Connection.GetServer(_redis.Connection.GetEndPoints().First());
        var keys = server.Keys(pattern: "chat:tournament:*");

        foreach (var key in keys)
        {
            await CleanupOldChatMessages(db, key.ToString(), daysOld);
        }
    }

    private async Task CleanupExpiredSessions(IDatabase db)
    {
        _logger.LogInformation("Cleaning up expired sessions...");

        var server = _redis.Connection.GetServer(_redis.Connection.GetEndPoints().First());
        var sessionKeys = server.Keys(pattern: "session:*");

        int deletedCount = 0;

        foreach (var key in sessionKeys)
        {
            var ttl = await db.KeyTimeToLiveAsync(key);
            
            // Ako nema TTL (TTL = null) ili je istekao, obriši
            if (!ttl.HasValue || ttl.Value.TotalSeconds <= 0)
            {
                await db.KeyDeleteAsync(key);
                deletedCount++;
            }
        }

        _logger.LogInformation("Deleted {Count} expired sessions", deletedCount);
    }
}
