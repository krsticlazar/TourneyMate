using Microsoft.AspNetCore.Mvc;
using Neo4jClient.Cypher;
using Microsoft.AspNetCore.Authorization;
using TourneyMate.Api.Dtos;
using TourneyMate.Api.Services;
using TourneyMate.Api.Models;
using System.Security.Claims;

namespace TourneyMate.Api.Controllers;

[ApiController]
[Route("api/neo4j")]
public sealed class Neo4jController : ControllerBase
{
    private readonly Neo4jService _neo;

    public Neo4jController(Neo4jService neo) => _neo = neo;

    // Node classes moved to Models/Neo4jNodes.cs

    // -------------------------
    // BASIC CHECKS
    // -------------------------
    [HttpGet("ping")]
    public async Task<IActionResult> Ping()
    {
        var client = await _neo.ClientAsync();
        var res = await client.Cypher
            .Return(() => Return.As<int>("1"))
            .ResultsAsync;

        return Ok(new { ok = true, result = res.FirstOrDefault() });
    }

    [HttpGet("counts")]
    public async Task<IActionResult> Counts()
    {
        var client = await _neo.ClientAsync();

        var nodes = (await client.Cypher
            .Match("(n)")
            .Return(() => Return.As<long>("count(n)"))
            .ResultsAsync).Single();

        var rels = (await client.Cypher
            .Match("()-[r]->()")
            .Return(() => Return.As<long>("count(r)"))
            .ResultsAsync).Single();

        return Ok(new { nodes, rels });
    }

    // -------------------------
    // CREATE NODES (optional, for manual testing)
    // -------------------------
    [Authorize]
    [HttpPost("teams")]
    public async Task<IActionResult> CreateTeam([FromBody] ApiDtos.CreateTeamDto dto)
    {
        var client = await _neo.ClientAsync();

        await client.Cypher
            .Merge("(t:Team { teamId: $id })")
            .OnCreate()
            .Set("t.name = $name, t.sport = $sport")
            .WithParams(new { id = dto.TeamId, name = dto.Name, sport = dto.Sport })
            .ExecuteWithoutResultsAsync();

        return Ok();
    }

    [Authorize]
    [HttpPost("tournaments")]
    public async Task<IActionResult> CreateTournament([FromBody] ApiDtos.CreateTournamentDto dto)
    {
        var client = await _neo.ClientAsync();

        await client.Cypher
            .Merge("(tr:Tournament { tournamentId: $id })")
            .OnCreate()
            .Set("tr.name = $name, tr.sport = $sport, tr.status = $status")
            .WithParams(new { id = dto.TournamentId, name = dto.Name, sport = dto.Sport, status = dto.Status })
            .ExecuteWithoutResultsAsync();

        return Ok();
    }

    // -------------------------
    // READ LISTS (quick inspect)
    // -------------------------
    [HttpGet("teams")]
    public async Task<IActionResult> ListTeams()
    {
        var client = await _neo.ClientAsync();

        var teams = await client.Cypher
            .Match("(t:Team)")
            .Return(t => t.As<TeamNode>())
            .ResultsAsync;

        return Ok(teams.Select(t => new { TeamId = t.teamId, Name = t.name, Sport = t.sport }));
    }

    [HttpGet("tournaments")]
    public async Task<IActionResult> ListTournaments()
    {
        var client = await _neo.ClientAsync();

        var trs = await client.Cypher
            .Match("(tr:Tournament)")
            .Return(tr => tr.As<TournamentNode>())
            .ResultsAsync;

        return Ok(trs.Select(tr => new { TournamentId = tr.tournamentId, Name = tr.name, Sport = tr.sport, Status = tr.status }));
    }

    // -------------------------
    // RELATIONS (what you need for Swagger tests)
    // -------------------------

    // Player -> Team  (MEMBER_OF)  (+ optional CAPTAIN_OF)
    // Example: POST /api/neo4j/players/viewer15/join/team_ft1?captain=false
    [Authorize]
    [HttpPost("players/{playerId}/join/{teamId}")]
    public async Task<IActionResult> JoinTeam(string playerId, string teamId, [FromQuery] bool captain = false)
    {
        var client = await _neo.ClientAsync();

        // ensure nodes exist (for seeded data they do)
        var pCount = (await client.Cypher
            .Match("(p:Player { playerId: $pid })")
            .WithParam("pid", playerId)
            .Return(() => Return.As<long>("count(p)"))
            .ResultsAsync).Single();

        if (pCount == 0) return NotFound(new { error = $"Player '{playerId}' not found." });

        var tCount = (await client.Cypher
            .Match("(t:Team { teamId: $tid })")
            .WithParam("tid", teamId)
            .Return(() => Return.As<long>("count(t)"))
            .ResultsAsync).Single();

        if (tCount == 0) return NotFound(new { error = $"Team '{teamId}' not found." });

        await client.Cypher
            .Match("(p:Player { playerId: $pid })", "(t:Team { teamId: $tid })")
            .WithParams(new { pid = playerId, tid = teamId })
            .Merge("(p)-[:MEMBER_OF]->(t)")
            .ExecuteWithoutResultsAsync();

        if (captain)
        {
            await client.Cypher
                .Match("(p:Player { playerId: $pid })", "(t:Team { teamId: $tid })")
                .WithParams(new { pid = playerId, tid = teamId })
                .Merge("(p)-[:CAPTAIN_OF]->(t)")
                .ExecuteWithoutResultsAsync();
        }

        return Ok(new { ok = true, playerId, teamId, captain });
    }

    // Team -> Tournament (ENTERS)
    // Example: POST /api/neo4j/teams/team_ch2/enter/t_chs_1?approved=true
    [Authorize]
    [HttpPost("teams/{teamId}/enter/{tournamentId}")]
    public async Task<IActionResult> EnterTournament(string teamId, string tournamentId, [FromQuery] bool approved = true)
    {
        var client = await _neo.ClientAsync();

        var tCount = (await client.Cypher
            .Match("(t:Team { teamId: $tid })")
            .WithParam("tid", teamId)
            .Return(() => Return.As<long>("count(t)"))
            .ResultsAsync).Single();

        if (tCount == 0) return NotFound(new { error = $"Team '{teamId}' not found." });

        var trCount = (await client.Cypher
            .Match("(tr:Tournament { tournamentId: $trid })")
            .WithParam("trid", tournamentId)
            .Return(() => Return.As<long>("count(tr)"))
            .ResultsAsync).Single();

        if (trCount == 0) return NotFound(new { error = $"Tournament '{tournamentId}' not found." });

        await client.Cypher
            .Match("(t:Team { teamId: $tid })", "(tr:Tournament { tournamentId: $trid })")
            .WithParams(new { tid = teamId, trid = tournamentId, approved })
            .Merge("(t)-[e:ENTERS]->(tr)")
            .Set("e.approved = $approved")
            .ExecuteWithoutResultsAsync();

        return Ok(new { ok = true, teamId, tournamentId, approved });
    }

    // -------------------------
    // QUERIES (for quick 5-min verification)
    // -------------------------

    // GET /api/neo4j/teams/{teamId}/players
    [HttpGet("teams/{teamId}/captain")]
    public async Task<IActionResult> CaptainOfTeam(string teamId)
    {
        var client = await _neo.ClientAsync();

        var captain = await client.Cypher
            .Match("(p:Player)-[:CAPTAIN_OF]->(t:Team { teamId: $tid })")
            .WithParam("tid", teamId)
            .Return(p => p.As<PlayerNode>())
            .ResultsAsync;

        var c = captain.FirstOrDefault();
        if (c is null) return NotFound(new { error = $"No captain found for team '{teamId}'." });

        return Ok(new { TeamId = teamId, Captain = new { PlayerId = c.playerId, Name = c.name } });
    }


    // GET /api/neo4j/tournaments/t_fut_1/teams
    [HttpGet("tournaments/{tournamentId}/teams")]
    public async Task<IActionResult> TeamsOfTournament(string tournamentId)
    {
        var client = await _neo.ClientAsync();

        var teams = await client.Cypher
            .Match("(t:Team)-[:ENTERS]->(tr:Tournament { tournamentId: $trid })")
            .WithParam("trid", tournamentId)
            .Return(t => t.As<TeamNode>())
            .ResultsAsync;

        return Ok(new
        {
            TournamentId = tournamentId,
            Teams = teams.Select(t => new { TeamId = t.teamId, Name = t.name, Sport = t.sport })
        });
    }

    [Authorize]
    [HttpPost("teams/{teamId}/apply/{tournamentId}")]
    public async Task<IActionResult> Apply(string teamId, string tournamentId)
    {
        var client = await _neo.ClientAsync();

        // dozvoli samo na Open
        var status = (await client.Cypher
            .Match("(tr:Tournament { tournamentId: $trid })")
            .WithParam("trid", tournamentId)
            .Return(tr => tr.As<TournamentNode>().status)
            .ResultsAsync).FirstOrDefault();

        if (status is null) return NotFound(new { error = $"Tournament '{tournamentId}' not found." });
        if (!string.Equals(status, "Open", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { error = $"Tournament '{tournamentId}' is '{status}'. Apply is allowed only for Open." });

        await client.Cypher
            .Match("(t:Team { teamId: $tid })", "(tr:Tournament { tournamentId: $trid })")
            .WithParams(new { tid = teamId, trid = tournamentId })
            .Merge("(t)-[a:APPLIED_FOR]->(tr)")
            .Set("a.status = 'Pending', a.createdAt = datetime()")
            .ExecuteWithoutResultsAsync();

        return Ok(new { ok = true, teamId, tournamentId, status = "Pending" });
    }

    [HttpGet("tournaments/{tournamentId}/applications")]
    public async Task<IActionResult> ListApplications(string tournamentId, [FromQuery] string status = "Pending")
    {
        var client = await _neo.ClientAsync();

        var apps = await client.Cypher
            .Match("(t:Team)-[a:APPLIED_FOR]->(tr:Tournament { tournamentId: $trid })")
            .WithParam("trid", tournamentId)
            .Where("a.status = $st")
            .WithParam("st", status)
            .Return((t, a) => new
            {
                TeamId = t.As<TeamNode>().teamId,
                Name = t.As<TeamNode>().name,
                Sport = t.As<TeamNode>().sport,
                Status = Return.As<string>("a.status"),
                CreatedAt = Return.As<string>("toString(a.createdAt)")
            })
            .ResultsAsync;

        return Ok(new { TournamentId = tournamentId, Status = status, Applications = apps });
    }

    [Authorize]
    [HttpPost("hosts/{hostUsername}/tournaments/{tournamentId}/applications/{teamId}/approve")]
    public async Task<IActionResult> Approve(string hostUsername, string tournamentId, string teamId)
    {
        var client = await _neo.ClientAsync();

        var caller = User.Identity?.Name;
        var callerRole = User.FindFirst(ClaimTypes.Role)?.Value;
        if (!string.Equals(caller, hostUsername, StringComparison.OrdinalIgnoreCase)
            && !string.Equals(callerRole, "admin", StringComparison.OrdinalIgnoreCase))
            return StatusCode(403, new { error = "Caller is not authorized as the specified host." });

        // host mora da hostuje taj turnir
        var can = (await client.Cypher
            .Match("(h:User { username: $h })-[:HOSTS|COHOSTS]->(tr:Tournament { tournamentId: $trid })")
            .WithParams(new { h = hostUsername, trid = tournamentId })
            .Return(() => Return.As<long>("count(h)"))
            .ResultsAsync).Single();

        if (can == 0) return StatusCode(403, new { error = "Host is not assigned to this tournament." });

        // approve: APPLIED_FOR -> Approved + napravi ENTERS (approved=true)
        await client.Cypher
            .Match("(t:Team { teamId: $tid })-[a:APPLIED_FOR]->(tr:Tournament { tournamentId: $trid })")
            .WithParams(new { tid = teamId, trid = tournamentId })
            .Set("a.status = 'Approved', a.reviewedAt = datetime()")
            .Merge("(t)-[e:ENTERS]->(tr)")
            .Set("e.approved = true")
            .ExecuteWithoutResultsAsync();

        return Ok(new { ok = true, hostUsername, tournamentId, teamId, result = "Approved" });
    }

    [Authorize]
    [HttpPost("hosts/{hostUsername}/tournaments/{tournamentId}/applications/{teamId}/reject")]
    public async Task<IActionResult> Reject(string hostUsername, string tournamentId, string teamId)
    {
        var client = await _neo.ClientAsync();

        var caller = User.Identity?.Name;
        var callerRole = User.FindFirst(ClaimTypes.Role)?.Value;
        if (!string.Equals(caller, hostUsername, StringComparison.OrdinalIgnoreCase)
            && !string.Equals(callerRole, "admin", StringComparison.OrdinalIgnoreCase))
            return StatusCode(403, new { error = "Caller is not authorized as the specified host." });

        var can = (await client.Cypher
            .Match("(h:User { username: $h })-[:HOSTS|COHOSTS]->(tr:Tournament { tournamentId: $trid })")
            .WithParams(new { h = hostUsername, trid = tournamentId })
            .Return(() => Return.As<long>("count(h)"))
            .ResultsAsync).Single();

        if (can == 0) return StatusCode(403, new { error = "Host is not assigned to this tournament." });

        await client.Cypher
            .Match("(t:Team { teamId: $tid })-[a:APPLIED_FOR]->(tr:Tournament { tournamentId: $trid })")
            .WithParams(new { tid = teamId, trid = tournamentId })
            .Set("a.status = 'Rejected', a.reviewedAt = datetime()")
            .ExecuteWithoutResultsAsync();

        return Ok(new { ok = true, hostUsername, tournamentId, teamId, result = "Rejected" });
    }




}
