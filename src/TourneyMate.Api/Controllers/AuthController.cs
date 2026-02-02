using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TourneyMate.Api.Services;
using TourneyMate.Api.Models;
using TourneyMate.Redis.Repositories;

namespace TourneyMate.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly Neo4jService _neo;
    private readonly SessionRepository _sessions;

    public AuthController(Neo4jService neo, SessionRepository sessions)
    {
        _neo = neo;
        _sessions = sessions;
    }

    // UserNode moved to Models/Neo4jNodes.cs


    public sealed record LoginRequest(string Username, string Password);
    public sealed record RegisterRequest(string Username, string Password, string DisplayName);

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Username) || string.IsNullOrWhiteSpace(req.Password) || string.IsNullOrWhiteSpace(req.DisplayName))
            return BadRequest(new { error = "All fields are required." });

        if (req.Password.Length < 6)
            return BadRequest(new { error = "Password must be at least 6 characters." });

        var client = await _neo.ClientAsync();

        // Proveri da li username već postoji
        var existing = await client.Cypher
            .Match("(u:User { username: $un })")
            .WithParam("un", req.Username)
            .Return(u => u.As<UserNode>())
            .ResultsAsync;

        if (existing.Any())
            return Conflict(new { error = "Username already exists." });

        // Kreiraj Player node (po defaultu Viewer role)
        var playerId = req.Username; // Koristimo username kao playerId
        await client.Cypher
            .Create("(p:Player { playerId: $pid, name: $name })")
            .WithParam("pid", playerId)
            .WithParam("name", req.DisplayName)
            .ExecuteWithoutResultsAsync();

        // Kreiraj User node
        await client.Cypher
            .Create("(u:User { username: $un, password: $pw, displayName: $dn, role: 'Viewer' })")
            .WithParam("un", req.Username)
            .WithParam("pw", req.Password)
            .WithParam("dn", req.DisplayName)
            .ExecuteWithoutResultsAsync();

        return Ok(new { ok = true, message = "User registered successfully." });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Username) || string.IsNullOrWhiteSpace(req.Password))
            return BadRequest(new { error = "Username and password are required." });

        var client = await _neo.ClientAsync();

        var users = await client.Cypher
            .Match("(u:User { username: $un })")
            .WithParam("un", req.Username)
            .Where("u.password = $pw")
            .WithParam("pw", req.Password)
            .Return(u => u.As<UserNode>())
            .ResultsAsync;

        var u = users.FirstOrDefault();
        if (u is null) return Unauthorized(new { error = "Invalid credentials." });

        var token = Guid.NewGuid().ToString("N");
        var ttl = TimeSpan.FromHours(1);

        await _sessions.SetAsync(token, new SessionUser(
            Username: u.username,
            DisplayName: u.displayName,
            Role: u.role
        ), ttl);

        return Ok(new
        {
            token,
            expiresInSeconds = (int)ttl.TotalSeconds,
            user = new { username = u.username, displayName = u.displayName, role = u.role }
        });

    }

    [Authorize]
    [HttpGet("me")]
    public IActionResult Me()
    {
        return Ok(new
        {
            username = User.Identity?.Name,
            displayName = User.FindFirst("displayName")?.Value,
            role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value
        });
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        var token = User.FindFirst("token")?.Value;
        if (!string.IsNullOrWhiteSpace(token))
            await _sessions.DeleteAsync(token);

        return Ok(new { ok = true });
    }
}
