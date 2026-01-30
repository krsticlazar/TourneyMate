using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TourneyMate.Api.Models;
using TourneyMate.Api.Services;

namespace TourneyMate.Api.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = "Admin")]
public sealed class AdminController : ControllerBase
{
    private readonly Neo4jService _neo;

    public AdminController(Neo4jService neo)
    {
        _neo = neo;
    }

    /// <summary>
    /// Admin only - lista svih usera
    /// </summary>
    [HttpGet("users")]
    public async Task<IActionResult> GetAllUsers()
    {
        var client = await _neo.ClientAsync();

        var users = await client.Cypher
            .Match("(u:User)")
            .Return(u => new
            {
                Username = u.As<UserNode>().username,
                DisplayName = u.As<UserNode>().displayName,
                Role = u.As<UserNode>().role
            })
            .OrderBy("u.username")
            .ResultsAsync;

        return Ok(users.ToList());
    }

    public sealed record SetRoleRequest(string Role);

    /// <summary>
    /// Admin only - postavi role za usera (Viewer/Host/Admin)
    /// </summary>
    [HttpPost("users/{username}/role")]
    public async Task<IActionResult> SetUserRole(string username, [FromBody] SetRoleRequest req)
    {
        if (string.IsNullOrWhiteSpace(username))
            return BadRequest(new { error = "Username is required." });

        if (string.IsNullOrWhiteSpace(req.Role))
            return BadRequest(new { error = "Role is required." });

        var validRoles = new[] { "Viewer", "Host", "Admin" };
        if (!validRoles.Contains(req.Role, StringComparer.OrdinalIgnoreCase))
            return BadRequest(new { error = "Invalid role. Must be one of: Viewer, Host, Admin." });

        var client = await _neo.ClientAsync();

        // Proveri da li user postoji
        var userExists = await client.Cypher
            .Match("(u:User { username: $un })")
            .WithParam("un", username)
            .Return(u => u.As<UserNode>())
            .ResultsAsync;

        if (!userExists.Any())
            return NotFound(new { error = "User not found." });

        // Postavi novu rolu
        await client.Cypher
            .Match("(u:User { username: $un })")
            .WithParam("un", username)
            .Set("u.role = $role")
            .WithParam("role", req.Role)
            .ExecuteWithoutResultsAsync();

        return Ok(new
        {
            username,
            role = req.Role,
            updated = true
        });
    }
}
