using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TourneyMate.Api.Models;
using TourneyMate.Api.Services;
using Neo4jClient.Cypher;

namespace TourneyMate.Api.Controllers;

[ApiController]
[Route("api/teams")]
public sealed class TeamController : ControllerBase
{
    private readonly Neo4jService _neo;

    public TeamController(Neo4jService neo)
    {
        _neo = neo;
    }

    public sealed record CreateTeamRequest(string Name, string Sport);

    /// <summary>
    /// Viewer only - kreira tim i automatski postavi CAPTAIN_OF za ulogovanog usera
    /// </summary>
    [Authorize(Roles = "Viewer")]
    [HttpPost]
    public async Task<IActionResult> CreateTeam([FromBody] CreateTeamRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Name))
            return BadRequest(new { error = "Team name is required." });

        if (string.IsNullOrWhiteSpace(req.Sport))
            return BadRequest(new { error = "Sport is required." });

        var username = User.Identity?.Name;
        if (string.IsNullOrWhiteSpace(username))
            return Unauthorized(new { error = "Missing user identity." });

        var client = await _neo.ClientAsync();

        // Generiši jedinstveni teamId
        var teamId = $"team_{Guid.NewGuid():N}";

        // Kreiraj tim i odmah postavi CAPTAIN_OF
        await client.Cypher
            .Match("(p:Player { playerId: $pid })")
            .WithParam("pid", username)
            .Create("(t:Team { teamId: $tid, name: $name, sport: $sport })")
            .WithParam("tid", teamId)
            .WithParam("name", req.Name.Trim())
            .WithParam("sport", req.Sport.Trim())
            .Create("(p)-[:CAPTAIN_OF]->(t)")
            .ExecuteWithoutResultsAsync();

        return Ok(new
        {
            teamId,
            name = req.Name.Trim(),
            sport = req.Sport.Trim(),
            captainUsername = username
        });
    }

    // Viewer only, captain-only - apply tim za turnir
    [Authorize(Roles = "Viewer")]
    [HttpPost("{teamId}/apply/{tournamentId}")]
    public async Task<IActionResult> ApplyForTournament(string teamId, string tournamentId)
    {
        if (string.IsNullOrWhiteSpace(teamId))
            return BadRequest(new { error = "Team ID is required." });

        if (string.IsNullOrWhiteSpace(tournamentId))
            return BadRequest(new { error = "Tournament ID is required." });

        var username = User.Identity?.Name;
        if (string.IsNullOrWhiteSpace(username))
            return Unauthorized(new { error = "Missing user identity." });

        var client = await _neo.ClientAsync();

        // Proveri da li je user kapiten ovog tima
        var captainCheck = await client.Cypher
            .Match("(p:Player { playerId: $pid })-[:CAPTAIN_OF]->(t:Team { teamId: $tid })")
            .WithParam("pid", username)
            .WithParam("tid", teamId)
            .Return(t => t.As<TeamNode>())
            .ResultsAsync;

        var team = captainCheck.FirstOrDefault();
        if (team == null)
            return Forbid(); // User nije kapiten ovog tima

        // Proveri da li turnir postoji i dohvati info
        var tournamentResult = await client.Cypher
            .Match("(tr:Tournament { tournamentId: $trid })")
            .WithParam("trid", tournamentId)
            .Return(tr => tr.As<TournamentNode>())
            .ResultsAsync;

        var tournament = tournamentResult.FirstOrDefault();
        if (tournament == null)
            return NotFound(new { error = "Tournament not found." });

        // ✅ VALIDACIJA #1: Proveri da li je isti sport
        if (!team.sport.Equals(tournament.sport, StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { error = $"Team sport ({team.sport}) does not match tournament sport ({tournament.sport})." });

        // ✅ VALIDACIJA #2: Proveri da li je turnir "Finished"
        if (tournament.status.Equals("Finished", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { error = "Cannot apply to a finished tournament." });

        // Proveri da li tim već ima aplikaciju ili je već ušao
        var existingRel = await client.Cypher
            .Match("(t:Team { teamId: $tid })-[r]->(tr:Tournament { tournamentId: $trid })")
            .WithParam("tid", teamId)
            .WithParam("trid", tournamentId)
            .Where("type(r) IN ['APPLIED_FOR', 'ENTERS']")
            .Return(r => r.As<object>())
            .ResultsAsync;

        if (existingRel.Any())
            return Conflict(new { error = "Team has already applied or entered this tournament." });

        // Kreiraj APPLIED_FOR relaciju sa status=Pending
        await client.Cypher
            .Match("(t:Team { teamId: $tid }), (tr:Tournament { tournamentId: $trid })")
            .WithParam("tid", teamId)
            .WithParam("trid", tournamentId)
            .Merge("(t)-[r:APPLIED_FOR]->(tr)")
            .OnCreate()
            .Set("r.status = 'Pending'")
            .Set("r.createdAt = datetime()")
            .ExecuteWithoutResultsAsync();

        return Ok(new { ok = true, status = "Pending" });
    }

    /// <summary>
    /// Host only - lista svih pending aplikacija za turnir koji hostuje
    /// </summary>
    [Authorize(Roles = "Host")]
    [HttpGet("applications/{tournamentId}")]
    public async Task<IActionResult> GetApplications(string tournamentId, [FromQuery] string? status = "Pending")
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
            return Forbid(); // User ne hostuje ovaj turnir

        // Dohvati aplikacije
        var query = client.Cypher
            .Match("(t:Team)-[ap:APPLIED_FOR]->(tr:Tournament { tournamentId: $tid })")
            .WithParam("tid", tournamentId);

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where("ap.status = $status").WithParam("status", status);

        var apps = await query
            .Return((t, ap) => new
            {
                TeamId = t.As<TeamNode>().teamId,
                Name = t.As<TeamNode>().name,
                Sport = t.As<TeamNode>().sport,
                Status = Return.As<string>("ap.status"),
                CreatedAt = Return.As<DateTime?>("ap.createdAt")
            })
            .ResultsAsync;

        return Ok(apps.ToList());
    }

    /// <summary>
    /// Host only - approve application (pretvori APPLIED_FOR u ENTERS)
    /// </summary>
    [Authorize(Roles = "Host")]
    [HttpPost("applications/{tournamentId}/{teamId}/approve")]
    public async Task<IActionResult> ApproveApplication(string tournamentId, string teamId)
    {
        if (string.IsNullOrWhiteSpace(tournamentId))
            return BadRequest(new { error = "Tournament ID is required." });

        if (string.IsNullOrWhiteSpace(teamId))
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
            return Forbid();

        // Proveri da li postoji pending aplikacija
        var appCheck = await client.Cypher
            .Match("(t:Team { teamId: $teamId })-[ap:APPLIED_FOR { status: 'Pending' }]->(tr:Tournament { tournamentId: $tid })")
            .WithParam("teamId", teamId)
            .WithParam("tid", tournamentId)
            .Return(ap => ap.As<object>())
            .ResultsAsync;

        if (!appCheck.Any())
            return NotFound(new { error = "Pending application not found." });

        // Obriši APPLIED_FOR i kreiraj ENTERS
        await client.Cypher
            .Match("(t:Team { teamId: $teamId })-[ap:APPLIED_FOR]->(tr:Tournament { tournamentId: $tid })")
            .WithParam("teamId", teamId)
            .WithParam("tid", tournamentId)
            .Delete("ap")
            .With("t, tr")
            .Create("(t)-[e:ENTERS { approved: true, approvedAt: datetime() }]->(tr)")
            .ExecuteWithoutResultsAsync();

        return Ok(new { ok = true, status = "Approved" });
    }

    /// <summary>
    /// Host only - reject application (postavi status na Rejected)
    /// </summary>
    [Authorize(Roles = "Host")]
    [HttpPost("applications/{tournamentId}/{teamId}/reject")]
    public async Task<IActionResult> RejectApplication(string tournamentId, string teamId)
    {
        if (string.IsNullOrWhiteSpace(tournamentId))
            return BadRequest(new { error = "Tournament ID is required." });

        if (string.IsNullOrWhiteSpace(teamId))
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
            return Forbid();

        // Proveri da li postoji pending aplikacija
        var appCheck = await client.Cypher
            .Match("(t:Team { teamId: $teamId })-[ap:APPLIED_FOR { status: 'Pending' }]->(tr:Tournament { tournamentId: $tid })")
            .WithParam("teamId", teamId)
            .WithParam("tid", tournamentId)
            .Return(ap => ap.As<object>())
            .ResultsAsync;

        if (!appCheck.Any())
            return NotFound(new { error = "Pending application not found." });

        // Postavi status na Rejected
        await client.Cypher
            .Match("(t:Team { teamId: $teamId })-[ap:APPLIED_FOR]->(tr:Tournament { tournamentId: $tid })")
            .WithParam("teamId", teamId)
            .WithParam("tid", tournamentId)
            .Set("ap.status = 'Rejected'")
            .Set("ap.rejectedAt = datetime()")
            .ExecuteWithoutResultsAsync();

        return Ok(new { ok = true, status = "Rejected" });
    }
    
    // Viewer only - vidi timove gde je kapiten
    [Authorize(Roles = "Viewer")]
    [HttpGet("my-teams")]
    public async Task<IActionResult> GetMyTeams()
    {
        var username = User.Identity?.Name;
        if (string.IsNullOrWhiteSpace(username))
            return Unauthorized(new { error = "Missing user identity." });

        var client = await _neo.ClientAsync();

        var teams = await client.Cypher
            .Match("(p:Player { playerId: $pid })-[:CAPTAIN_OF]->(t:Team)")
            .WithParam("pid", username)
            .Return(t => new
            {
                TeamId = t.As<TeamNode>().teamId,
                Name = t.As<TeamNode>().name,
                Sport = t.As<TeamNode>().sport
            })
            .ResultsAsync;

        return Ok(teams.ToList());
    }
}
