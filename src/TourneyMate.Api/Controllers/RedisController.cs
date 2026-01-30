using Microsoft.AspNetCore.Mvc;
using TourneyMate.Api.Dtos;
using TourneyMate.Redis.Repositories;
using Microsoft.AspNetCore.Authorization;

namespace TourneyMate.Api.Controllers;

[ApiController]
[Route("api/redis")]
public sealed class RedisController : ControllerBase
{
    private readonly LeaderboardRepository _lb;

    public RedisController(LeaderboardRepository lb)
    {
        _lb = lb;
    }

    [HttpGet("ping")]
    public async Task<IActionResult> Ping()
    {
        // Ako ovo prođe bez exception-a, konekcija radi.
        await _lb.AddOrUpdateScoreAsync("ping-tournament", "teamA", 1);
        var top = await _lb.TopAsync("ping-tournament", 5);
        return Ok(new { ok = true, top });
    }

    [Authorize]
    [HttpPost("leaderboard/score")]
    public async Task<IActionResult> UpsertScore([FromBody] ApiDtos.UpsertScoreDto dto)
    {
        await _lb.AddOrUpdateScoreAsync(dto.TournamentId, dto.TeamId, dto.Score);
        return Ok();
    }

    [HttpGet("leaderboard/top")]
    public async Task<IActionResult> Top(
        [FromQuery(Name = "tournamentId")] string tournamentId,
        [FromQuery(Name = "topN")] int topN = 10)
    {
        if (string.IsNullOrWhiteSpace(tournamentId))
            return BadRequest(new { error = "tournamentId is required" });

        var top = await _lb.TopAsync(tournamentId, topN);
        return Ok(top);
    }

}
