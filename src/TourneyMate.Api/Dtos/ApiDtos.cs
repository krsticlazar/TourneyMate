using TourneyMate.Redis.Models;

namespace TourneyMate.Api.Dtos;

public static class ApiDtos
{
    // Redis - leaderboard
    public sealed record UpsertScoreDto(string TournamentId, string TeamId, double Score);

    // Redis - rate limit (ako ti treba)
    public sealed record RateLimitCheckDto(string Scope, string Id, int Max, int WindowSeconds);

    // Redis - chat
    public sealed record SendChatMessageDto(string UserId, string DisplayName, string Text);

    // Neo4j - minimal CRUD
    public sealed record CreatePlayerDto(string PlayerId, string Name);
    public sealed record CreateTeamDto(string TeamId, string Name, string Sport);

    public sealed record CreateTournamentDto(
        string TournamentId,
        string Name,
        string Sport,
        string Status // "Active" ili "Finished"
    );


    //-------------Home page---------------
    public sealed record HomeResponseDto(
        IReadOnlyList<HomeTournamentDto> Open,
        IReadOnlyList<HomeTournamentDto> Live,
        IReadOnlyList<HomeTournamentDto> Finished,
        IReadOnlyList<ChatMessageDto> GlobalChat
    );

    public sealed record HomeTournamentDto(
        string TournamentId,
        string Name,
        string Sport,
        string Status,
        IReadOnlyList<HomeHostDto> Hosts,
        IReadOnlyList<HomeTeamDto> EnteredTeams,
        IReadOnlyList<HomeApplicationDto> Applications,
        IReadOnlyList<HomeLeaderboardEntryDto> LeaderboardTop
    );

    public sealed record ChatMessageDto(
        string UserId,
        string DisplayName,
        string Text,
        DateTime TimestampUtc
    );

    public sealed record HomeHostDto(string Username, string DisplayName);

    public sealed record HomeTeamDto(string TeamId, string Name, string Sport);

    public sealed record HomeApplicationDto(string TeamId, string Name, string Sport, string Status);

    public sealed record HomeLeaderboardEntryDto(string TeamId, string? TeamName, double Score);

}
