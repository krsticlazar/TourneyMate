using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TourneyMate.Api.Constants;
using TourneyMate.Api.Dtos;
using TourneyMate.Redis.Models;
using TourneyMate.Redis.Repositories;

namespace TourneyMate.Api.Controllers;

[ApiController]
[Route("api/chat")]
public sealed class ChatController : ControllerBase
{
    private readonly ChatRepository _chat;

    public ChatController(ChatRepository chat) => _chat = chat;

    // Samo ulogovani mogu da salju poruke
    [Authorize]
    [HttpPost("global")]
    public async Task<IActionResult> SendGlobal([FromBody] ApiDtos.SendChatMessageDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Text))
            return BadRequest(new { error = "Text is required." });

        var userId = User.Identity?.Name;
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized(new { error = "Missing user identity." });

        var displayName = User.FindFirst("displayName")?.Value ?? userId;

        var msg = new ChatMessage(userId, displayName, dto.Text.Trim(), DateTimeOffset.UtcNow);
        await _chat.PushMessageAsync(RedisKeys.GlobalChat, msg, keepLast: 200);

        return Ok(new { ok = true });
    }

    // Svi mogu da citaju
    [AllowAnonymous]
    [HttpGet("global")]
    public async Task<IActionResult> GetGlobal([FromQuery] int last = 50)
    {
        var msgs = await _chat.GetLastAsync(RedisKeys.GlobalChat, last);
        return Ok(msgs);
    }

    // Samo ulogovani mogu da salju poruke
    [Authorize]
    [HttpPost("tournament/{tournamentId}")]
    public async Task<IActionResult> SendTournament(string tournamentId, [FromBody] ApiDtos.SendChatMessageDto dto)
    {
        if (string.IsNullOrWhiteSpace(tournamentId))
            return BadRequest(new { error = "tournamentId is required." });

        if (string.IsNullOrWhiteSpace(dto.Text))
            return BadRequest(new { error = "Text is required." });

        var userId = User.Identity?.Name;
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized(new { error = "Missing user identity." });

        var displayName = User.FindFirst("displayName")?.Value ?? userId;

        var msg = new ChatMessage(userId, displayName, dto.Text.Trim(), DateTimeOffset.UtcNow);
        await _chat.PushMessageAsync(RedisKeys.TournamentChat(tournamentId), msg, keepLast: 200);

        return Ok(new { ok = true });
    }

    // Svi mogu da citaju
    [AllowAnonymous]
    [HttpGet("tournament/{tournamentId}")]
    public async Task<IActionResult> GetTournament(string tournamentId, [FromQuery] int last = 50)
    {
        if (string.IsNullOrWhiteSpace(tournamentId))
            return BadRequest(new { error = "tournamentId is required." });

        var msgs = await _chat.GetLastAsync(RedisKeys.TournamentChat(tournamentId), last);
        return Ok(msgs);
    }
}
