namespace TourneyMate.Api.Models;

public sealed class TournamentNode
{
    public string tournamentId { get; set; } = "";
    public string name { get; set; } = "";
    public string sport { get; set; } = "";
    public string status { get; set; } = "";
}

public sealed class TeamNode
{
    public string teamId { get; set; } = "";
    public string name { get; set; } = "";
    public string sport { get; set; } = "";
}

public sealed class PlayerNode
{
    public string playerId { get; set; } = "";
    public string name { get; set; } = "";
}

public sealed class UserNode
{
    public string username { get; set; } = "";
    public string displayName { get; set; } = "";
    public string role { get; set; } = "";
}
