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

    // Host only - dodaj/ažuriraj bodove za tim u turniru
    [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Host")]
    [HttpPost("{tournamentId}/score")]
    public async Task<IActionResult> UpdateScore(
        string tournamentId,
        [FromBody] UpdateScoreRequest request)
    {
        if (string.IsNullOrWhiteSpace(tournamentId))
            return BadRequest(new { error = "Tournament ID is required." });

        if (string.IsNullOrWhiteSpace(request.TeamId))
            return BadRequest(new { error = "Team ID is required." });

        var username = User.Identity?.Name;
        if (string.IsNullOrWhiteSpace(username))
            return Unauthorized(new { error = "Missing user identity." });

        var client = await _neo.ClientAsync();

        // Proveri da li user hostuje ovaj turnir
        var hostCheck = await client.Cypher
            .Match("(h:User { username: $un })-[:HOSTS|COHOSTS]->(tr:Tournament { tournamentId: $tid })")
            .WithParam("un", username)
            .WithParam("tid", tournamentId)
            .Return(tr => tr.As<TournamentNode>())
            .ResultsAsync;

        if (!hostCheck.Any())
            return Forbid(); // User ne hostuje ovaj turnir

        // Proveri da li tim učestvuje u turniru
        var teamCheck = await client.Cypher
            .Match("(t:Team { teamId: $teamId })-[:ENTERS]->(tr:Tournament { tournamentId: $tid })")
            .WithParam("teamId", request.TeamId)
            .WithParam("tid", tournamentId)
            .Return(t => t.As<TeamNode>())
            .ResultsAsync;

        if (!teamCheck.Any())
            return NotFound(new { error = "Team is not participating in this tournament." });

        // Dodaj/ažuriraj bodove u Redis leaderboard
        await _lb.AddOrUpdateScoreAsync(tournamentId, request.TeamId, request.Score);

        return Ok(new { ok = true, teamId = request.TeamId, score = request.Score });
    }

    public sealed record UpdateScoreRequest(string TeamId, double Score);

    // Host only - kreiraj novi turnir
    [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Host")]
    [HttpPost("create")]
    public async Task<IActionResult> CreateTournament([FromBody] CreateTournamentRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(new { error = "Tournament name is required." });

        if (string.IsNullOrWhiteSpace(request.Sport))
            return BadRequest(new { error = "Sport is required." });

        var username = User.Identity?.Name;
        if (string.IsNullOrWhiteSpace(username))
            return Unauthorized(new { error = "Missing user identity." });

        var client = await _neo.ClientAsync();

        // Generiši tournamentId
        var tournamentId = $"t_{request.Sport.ToLower().Substring(0, 3)}_{Guid.NewGuid().ToString().Substring(0, 8)}";

        // Kreiraj Tournament node
        await client.Cypher
            .Create("(tr:Tournament $tournament)")
            .WithParam("tournament", new
            {
                tournamentId,
                name = request.Name,
                sport = request.Sport,
                status = "Open", // Novi turnir je Open
                createdAt = DateTime.UtcNow
            })
            .ExecuteWithoutResultsAsync();

        // Kreiraj HOSTS relaciju
        await client.Cypher
            .Match("(h:User { username: $un }), (tr:Tournament { tournamentId: $tid })")
            .WithParam("un", username)
            .WithParam("tid", tournamentId)
            .Create("(h)-[:HOSTS]->(tr)")
            .ExecuteWithoutResultsAsync();

        return Ok(new { tournamentId, name = request.Name, sport = request.Sport, status = "Open" });
    }

    // Host only - startuj turnir (Open -> Live)
    [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Host")]
    [HttpPost("{tournamentId}/start")]
    public async Task<IActionResult> StartTournament(string tournamentId)
    {
        if (string.IsNullOrWhiteSpace(tournamentId))
            return BadRequest(new { error = "Tournament ID is required." });

        var username = User.Identity?.Name;
        if (string.IsNullOrWhiteSpace(username))
            return Unauthorized(new { error = "Missing user identity." });

        var client = await _neo.ClientAsync();

        // Proveri da li user hostuje ovaj turnir
        var hostCheck = await client.Cypher
            .Match("(h:User { username: $un })-[:HOSTS|COHOSTS]->(tr:Tournament { tournamentId: $tid })")
            .WithParam("un", username)
            .WithParam("tid", tournamentId)
            .Return(tr => tr.As<TournamentNode>())
            .ResultsAsync;

        var tournament = hostCheck.FirstOrDefault();
        if (tournament == null)
            return Forbid();

        // Proveri broj učesnika prema sportu
        var teamsCount = await client.Cypher
            .Match("(t:Team)-[:ENTERS]->(tr:Tournament { tournamentId: $tid })")
            .WithParam("tid", tournamentId)
            .Return(() => Return.As<long>("count(t)"))
            .ResultsAsync;

        var count = teamsCount.FirstOrDefault();
        var minTeams = tournament.sport switch
        {
            "Football" => 4,
            "Basketball" => 4,
            "Chess" => 2,
            _ => 2
        };

        if (count < minTeams)
            return BadRequest(new { error = $"Tournament needs at least {minTeams} teams to start. Currently has {count} teams." });

        // Promeni status na Live
        await client.Cypher
            .Match("(tr:Tournament { tournamentId: $tid })")
            .WithParam("tid", tournamentId)
            .Set("tr.status = 'Live'")
            .ExecuteWithoutResultsAsync();

        // Inicijalizuj leaderboard - dodaj sve timove sa 0 bodova
        var teams = await client.Cypher
            .Match("(t:Team)-[:ENTERS]->(tr:Tournament { tournamentId: $tid })")
            .WithParam("tid", tournamentId)
            .Return(t => t.As<TeamNode>())
            .ResultsAsync;

        foreach (var team in teams)
        {
            await _lb.AddOrUpdateScoreAsync(tournamentId, team.teamId, 0);
        }

        return Ok(new { ok = true, status = "Live" });
    }

    // Host only - završi turnir (Live -> Finished)
    [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Host")]
    [HttpPost("{tournamentId}/finish")]
    public async Task<IActionResult> FinishTournament(string tournamentId)
    {
        if (string.IsNullOrWhiteSpace(tournamentId))
            return BadRequest(new { error = "Tournament ID is required." });

        var username = User.Identity?.Name;
        if (string.IsNullOrWhiteSpace(username))
            return Unauthorized(new { error = "Missing user identity." });

        var client = await _neo.ClientAsync();

        // Proveri da li user hostuje ovaj turnir
        var hostCheck = await client.Cypher
            .Match("(h:User { username: $un })-[:HOSTS|COHOSTS]->(tr:Tournament { tournamentId: $tid })")
            .WithParam("un", username)
            .WithParam("tid", tournamentId)
            .Return(tr => tr.As<TournamentNode>())
            .ResultsAsync;

        if (!hostCheck.Any())
            return Forbid();

        // Promeni status na Finished
        await client.Cypher
            .Match("(tr:Tournament { tournamentId: $tid })")
            .WithParam("tid", tournamentId)
            .Set("tr.status = 'Finished'")
            .ExecuteWithoutResultsAsync();

        return Ok(new { ok = true, status = "Finished" });
    }

    public sealed record CreateTournamentRequest(string Name, string Sport);
}
