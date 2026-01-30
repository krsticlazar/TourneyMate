using Microsoft.AspNetCore.Mvc;
using Neo4jClient.Cypher;
using TourneyMate.Api.Constants;
using TourneyMate.Api.Dtos;
using TourneyMate.Api.Models;
using TourneyMate.Api.Services;
using TourneyMate.Redis.Repositories;

namespace TourneyMate.Api.Controllers;

[ApiController]
[Route("api/home")]
public sealed class HomeController : ControllerBase
{
    private readonly Neo4jService _neo;
    private readonly LeaderboardRepository _lb;
    private readonly ChatRepository _chat;

    public HomeController(Neo4jService neo, LeaderboardRepository lb, ChatRepository chat)
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

    private sealed class HomeRow
    {
        public string TournamentId { get; set; } = "";
        public string Name { get; set; } = "";
        public string Sport { get; set; } = "";
        public string Status { get; set; } = "";

        public List<HostRow> Hosts { get; set; } = new();
        public List<TeamRow> EnteredTeams { get; set; } = new();
        public List<AppRow> Applications { get; set; } = new();
    }

    [HttpGet]
    public async Task<IActionResult> GetHome([FromQuery] int topN = 5, [FromQuery] int chatN = 30)
    {
        if (topN <= 0) topN = 5;
        if (topN > 50) topN = 50;

        if (chatN <= 0) chatN = 30;
        if (chatN > 200) chatN = 200;

        var client = await _neo.ClientAsync();

        var rows = await client.Cypher
            .Match("(tr:Tournament)")
            .OptionalMatch("(h:User)-[:HOSTS|COHOSTS]->(tr)")
            .OptionalMatch("(et:Team)-[:ENTERS]->(tr)")
            .OptionalMatch("(at:Team)-[ap:APPLIED_FOR]->(tr)")
            .With(@"
                tr,
                collect(distinct h { .username, .displayName }) as hosts,
                collect(distinct et { .teamId, .name, .sport }) as enteredTeams,
                collect(distinct at { .teamId, .name, .sport, status: ap.status }) as applications
            ")
            .Return(tr => new HomeRow
            {
                TournamentId = tr.As<TournamentNode>().tournamentId,
                Name = tr.As<TournamentNode>().name,
                Sport = tr.As<TournamentNode>().sport,
                Status = tr.As<TournamentNode>().status,
                Hosts = Return.As<List<HostRow>>("hosts"),
                EnteredTeams = Return.As<List<TeamRow>>("enteredTeams"),
                Applications = Return.As<List<AppRow>>("applications")
            })
            .ResultsAsync;

        var dtoList = new List<ApiDtos.HomeTournamentDto>();

        foreach (var r in rows)
        {
            var hosts = (r.Hosts ?? new())
                .Where(x => !string.IsNullOrWhiteSpace(x.username))
                .Select(x => new ApiDtos.HomeHostDto(x.username!, x.displayName ?? x.username!))
                .ToList();

            var entered = (r.EnteredTeams ?? new())
                .Where(x => !string.IsNullOrWhiteSpace(x.teamId))
                .Select(x => new ApiDtos.HomeTeamDto(x.teamId!, x.name ?? x.teamId!, x.sport ?? ""))
                .ToList();

            var apps = (r.Applications ?? new())
                .Where(x => !string.IsNullOrWhiteSpace(x.teamId))
                .Select(x => new ApiDtos.HomeApplicationDto(
                    x.teamId!, x.name ?? x.teamId!, x.sport ?? "", x.status ?? "Pending"))
                .ToList();

            var nameById = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var t in entered) nameById[t.TeamId] = t.Name;
            foreach (var a in apps) if (!nameById.ContainsKey(a.TeamId)) nameById[a.TeamId] = a.Name;

            var lb = await _lb.TopAsync(r.TournamentId, topN);
            var lbDto = lb.Select(e =>
            {
                nameById.TryGetValue(e.TeamId, out var teamName);
                return new ApiDtos.HomeLeaderboardEntryDto(e.TeamId, teamName, e.Score);
            }).ToList();

            dtoList.Add(new ApiDtos.HomeTournamentDto(
                TournamentId: r.TournamentId,
                Name: r.Name,
                Sport: r.Sport,
                Status: r.Status,
                Hosts: hosts,
                EnteredTeams: entered,
                Applications: apps,
                LeaderboardTop: lbDto
            ));
        }

        var open = dtoList.Where(x => x.Status.Equals("Open", StringComparison.OrdinalIgnoreCase)).ToList();
        var live = dtoList.Where(x => x.Status.Equals("Live", StringComparison.OrdinalIgnoreCase)).ToList();
        var finished = dtoList.Where(x => x.Status.Equals("Finished", StringComparison.OrdinalIgnoreCase)).ToList();

        var global = await _chat.GetLastAsync(RedisKeys.GlobalChat, chatN);
        var globalDto = global.Select(m => new ApiDtos.ChatMessageDto(
            m.UserId,
            m.DisplayName,
            m.Text,
            m.TimestampUtc.UtcDateTime
        )).ToList();

        return Ok(new ApiDtos.HomeResponseDto(open, live, finished, globalDto));
    }
}
