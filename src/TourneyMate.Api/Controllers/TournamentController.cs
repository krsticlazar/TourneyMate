using Microsoft.AspNetCore.Mvc;
using Neo4jClient.Cypher;
using TourneyMate.Api.Constants;
using TourneyMate.Api.Dtos;
using TourneyMate.Api.Models;
using TourneyMate.Api.Services;
using TourneyMate.Redis.Repositories;

namespace TourneyMate.Api.Controllers;

[ApiController]
[Route("api/tournaments")]
public sealed class TournamentController : ControllerBase
{
    private readonly Neo4jService _neo;
    private readonly LeaderboardRepository _lb;
    private readonly ChatRepository _chat;

    public TournamentController(Neo4jService neo, LeaderboardRepository lb, ChatRepository chat)
    {
        _neo = neo;
        _lb = lb;
        _chat = chat;
    }

    private sealed class HostRow
    {
        public string? username { get; set; }
        public string? displayName { get; set; }
    }

    private sealed class TeamRow
    {
        public string? teamId { get; set; }
        public string? name { get; set; }
        public string? sport { get; set; }
    }

    private sealed class AppRow
    {
        public string? teamId { get; set; }
        public string? name { get; set; }
        public string? sport { get; set; }
        public string? status { get; set; }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetTournament(
        string id,
        [FromQuery] int topN = 10,
        [FromQuery] int chatN = 50)
    {
        if (string.IsNullOrWhiteSpace(id))
            return BadRequest(new { error = "Tournament ID is required." });

        if (topN <= 0) topN = 10;
        if (topN > 50) topN = 50;

        if (chatN <= 0) chatN = 50;
        if (chatN > 200) chatN = 200;

        var client = await _neo.ClientAsync();

        // Fetch tournament info + hosts + entered teams + pending applications
        var result = await client.Cypher
            .Match("(tr:Tournament { tournamentId: $tid })")
            .WithParam("tid", id)
            .OptionalMatch("(h:User)-[:HOSTS|COHOSTS]->(tr)")
            .OptionalMatch("(et:Team)-[:ENTERS]->(tr)")
            .OptionalMatch("(at:Team)-[ap:APPLIED_FOR]->(tr)")
            .With(@"
                tr,
                collect(distinct h { .username, .displayName }) as hosts,
                collect(distinct et { .teamId, .name, .sport }) as enteredTeams,
                collect(distinct at { .teamId, .name, .sport, status: ap.status }) as applications
            ")
            .Return((tr, hosts, enteredTeams, applications) => new
            {
                Tournament = tr.As<TournamentNode>(),
                Hosts = Return.As<List<HostRow>>("hosts"),
                EnteredTeams = Return.As<List<TeamRow>>("enteredTeams"),
                Applications = Return.As<List<AppRow>>("applications")
            })
            .ResultsAsync;

        var data = result.FirstOrDefault();
        if (data?.Tournament == null)
            return NotFound(new { error = "Tournament not found." });

        var tournament = data.Tournament;

        // Map hosts
        var hosts = (data.Hosts ?? new())
            .Where(x => !string.IsNullOrWhiteSpace(x.username))
            .Select(x => new ApiDtos.HomeHostDto(x.username!, x.displayName ?? x.username!))
            .ToList();

        // Map entered teams
        var entered = (data.EnteredTeams ?? new())
            .Where(x => !string.IsNullOrWhiteSpace(x.teamId))
            .Select(x => new ApiDtos.HomeTeamDto(x.teamId!, x.name ?? x.teamId!, x.sport ?? ""))
            .ToList();

        // Map applications
        var apps = (data.Applications ?? new())
            .Where(x => !string.IsNullOrWhiteSpace(x.teamId))
            .Select(x => new ApiDtos.HomeApplicationDto(
                x.teamId!, x.name ?? x.teamId!, x.sport ?? "", x.status ?? "Pending"))
            .ToList();

        // Build teamId -> name mapping
        var nameById = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var t in entered) nameById[t.TeamId] = t.Name;
        foreach (var a in apps) if (!nameById.ContainsKey(a.TeamId)) nameById[a.TeamId] = a.Name;

        // Fetch leaderboard from Redis
        var lb = await _lb.TopAsync(tournament.tournamentId, topN);
        var lbDto = lb.Select(e =>
        {
            nameById.TryGetValue(e.TeamId, out var teamName);
            return new ApiDtos.HomeLeaderboardEntryDto(e.TeamId, teamName, e.Score);
        }).ToList();

        // Fetch tournament chat from Redis
        var chat = await _chat.GetLastAsync(RedisKeys.TournamentChat(tournament.tournamentId), chatN);
        var chatDto = chat.Select(m => new ApiDtos.ChatMessageDto(
            m.UserId,
            m.DisplayName,
            m.Text,
            m.TimestampUtc.UtcDateTime
        )).ToList();

        return Ok(new
        {
            tournamentId = tournament.tournamentId,
            name = tournament.name,
            sport = tournament.sport,
            status = tournament.status,
            hosts,
            enteredTeams = entered,
            applications = apps,
            leaderboard = lbDto,
            chat = chatDto
        });
    }
}
