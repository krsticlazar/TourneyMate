using StackExchange.Redis;
using TourneyMate.Redis.Infrastructure;

namespace TourneyMate.Redis.Repositories;

public sealed class LeaderboardRepository
{
    public sealed record LeaderboardEntry(string TeamId, double Score);

    private readonly RedisContext _ctx;

    public LeaderboardRepository(RedisContext ctx) => _ctx = ctx;

    private static string Key(string tournamentId) => $"tm:lb:{tournamentId}";

    public Task AddOrUpdateScoreAsync(string tournamentId, string teamId, double score)
        => _ctx.Db.SortedSetAddAsync(Key(tournamentId), teamId, score);

    public async Task<IReadOnlyList<LeaderboardEntry>> TopAsync(string tournamentId, int topN = 10)
    {
        var entries = await _ctx.Db.SortedSetRangeByRankWithScoresAsync(
            Key(tournamentId), 0, topN - 1, Order.Descending);

        return entries
            .Select(e => new LeaderboardEntry(e.Element.ToString(), e.Score))
            .ToList();
    }

}
