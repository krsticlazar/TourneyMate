namespace TourneyMate.Api.Constants;

public static class RedisKeys
{
    public static string Leaderboard(string tournamentId) => $"tm:lb:{tournamentId}";

    // Chat kanali
    public const string GlobalChat = "tm:chat:global";
    public static string TournamentChat(string tournamentId) => $"tm:chat:tournament:{tournamentId}";
}
