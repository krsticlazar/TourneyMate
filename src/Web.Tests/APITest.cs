namespace Web.Tests;

[TestFixture]
public class APITest : PlaywrightTest
{
    private const string ApiUrl = "http://localhost:5125";
    private IAPIRequestContext _request = null!;

    [SetUp]
    public async Task SetUpApi()
    {
        var headers = new Dictionary<string, string>
        {
            { "Accept", "application/json" }
        };

        _request = await Playwright.APIRequest.NewContextAsync(new()
        {
            BaseURL = ApiUrl,
            ExtraHTTPHeaders = headers
        });
    }

    [TearDown]
    public async Task TearDownApi()
    {
        await _request.DisposeAsync();
    }

    private static Dictionary<string, string> AuthHeaders(string token) => new()
    {
        { "Authorization", $"Bearer {token}" }
    };

    private static string UniqueId(string prefix, int len = 10)
    {
        var raw = $"{prefix}_{Guid.NewGuid():N}";
        return raw.Substring(0, Math.Min(raw.Length, len));
    }

    private async Task<JsonElement> ReadJson(IAPIResponse response)
    {
        var text = await response.TextAsync();
        using var doc = JsonDocument.Parse(text);
        return doc.RootElement.Clone();
    }

    private async Task<string> PrijaviSeVratiToken(string username, string password)
    {
        await using var response = await _request.PostAsync("/api/auth/login", new()
        {
            DataObject = new
            {
                username,
                password
            }
        });

        if (response.Status != 200)
        {
            Assert.Fail($"Login neuspesan ({response.Status}) {response.StatusText}");
        }

        var root = await ReadJson(response);
        var token = root.GetProperty("token").GetString();

        if (string.IsNullOrWhiteSpace(token))
        {
            Assert.Fail("Token nije vracen u login odgovoru.");
        }

        return token!;
    }

    private async Task<(string Username, string Password, string DisplayName, string Token)> RegistrujNovogViewer(string prefix = "pwv")
    {
        var username = UniqueId(prefix, 14);
        var password = "test123";
        var displayName = $"User_{username}";

        await using var registerResponse = await _request.PostAsync("/api/auth/register", new()
        {
            DataObject = new
            {
                username,
                password,
                displayName
            }
        });

        if (registerResponse.Status != 200)
        {
            Assert.Fail($"Registracija nije uspela ({registerResponse.Status}) {registerResponse.StatusText}");
        }

        var token = await PrijaviSeVratiToken(username, password);
        return (username, password, displayName, token);
    }

    private async Task<(string TournamentId, string Name, string Sport)> KreirajTurnirKaoHost(string hostToken, string sport = "Chess")
    {
        var name = $"{sport}_PW_{Guid.NewGuid():N}".Substring(0, 20);
        await using var response = await _request.PostAsync("/api/tournaments/create", new()
        {
            Headers = AuthHeaders(hostToken),
            DataObject = new
            {
                name,
                sport
            }
        });

        if (response.Status != 200)
        {
            Assert.Fail($"CreateTournament pao ({response.Status}) {response.StatusText}");
        }

        var root = await ReadJson(response);
        var tournamentId = root.GetProperty("tournamentId").GetString();
        Assert.That(tournamentId, Is.Not.Null.And.Not.Empty);
        return (tournamentId!, name, sport);
    }

    private async Task<(string TeamId, string TeamName, string Sport)> KreirajTimKaoViewer(string viewerToken, string sport = "Chess")
    {
        var teamName = $"Team_{Guid.NewGuid():N}".Substring(0, 16);
        await using var response = await _request.PostAsync("/api/teams", new()
        {
            Headers = AuthHeaders(viewerToken),
            DataObject = new
            {
                name = teamName,
                sport
            }
        });

        if (response.Status != 200)
        {
            Assert.Fail($"CreateTeam pao ({response.Status}) {response.StatusText}");
        }

        var root = await ReadJson(response);
        var teamId = root.GetProperty("teamId").GetString();
        Assert.That(teamId, Is.Not.Null.And.Not.Empty);
        return (teamId!, teamName, sport);
    }

    [Test]
    public async Task LoginTest()
    {
        var token = await PrijaviSeVratiToken("viewer01", "view123");
        Assert.That(token.Length, Is.GreaterThan(10));
    }

    [Test]
    public async Task LoginPogresnaLozinkaTest()
    {
        await using var response = await _request.PostAsync("/api/auth/login", new()
        {
            DataObject = new
            {
                username = "viewer01",
                password = "pogresna_lozinka"
            }
        });

        Assert.That(response.Status, Is.EqualTo(401));
    }

    [Test]
    public async Task RegisterValidacijaLozinkeTest()
    {
        var username = UniqueId("badreg", 12);

        await using var response = await _request.PostAsync("/api/auth/register", new()
        {
            DataObject = new
            {
                username,
                password = "123",
                displayName = "BadUser"
            }
        });

        Assert.That(response.Status, Is.EqualTo(400));
        var root = await ReadJson(response);
        Assert.That(root.GetProperty("error").GetString(), Does.Contain("Password"));
    }

    [Test]
    public async Task AuthMeTest()
    {
        var token = await PrijaviSeVratiToken("viewer01", "view123");

        await using var response = await _request.GetAsync("/api/auth/me", new()
        {
            Headers = AuthHeaders(token)
        });

        Assert.That(response.Status, Is.EqualTo(200));
        var root = await ReadJson(response);
        Assert.Multiple(() =>
        {
            Assert.That(root.GetProperty("username").GetString(), Is.EqualTo("viewer01"));
            Assert.That(root.GetProperty("role").GetString(), Is.EqualTo("Viewer"));
        });
    }

    [Test]
    public async Task LogoutInvalidiraTokenTest()
    {
        var user = await RegistrujNovogViewer("lgout");

        await using var logout = await _request.PostAsync("/api/auth/logout", new()
        {
            Headers = AuthHeaders(user.Token)
        });

        Assert.That(logout.Status, Is.EqualTo(200));

        await using var me = await _request.GetAsync("/api/auth/me", new()
        {
            Headers = AuthHeaders(user.Token)
        });

        Assert.That(me.Status, Is.EqualTo(401));
    }

    [Test]
    public async Task HomeApiTest()
    {
        await using var response = await _request.GetAsync("/api/home?topN=5&chatN=30");

        if (response.Status != 200)
        {
            Assert.Fail($"Code: {response.Status} - {response.StatusText}");
        }

        var root = await ReadJson(response);

        Assert.Multiple(() =>
        {
            Assert.That(root.TryGetProperty("open", out _), Is.True);
            Assert.That(root.TryGetProperty("live", out _), Is.True);
            Assert.That(root.TryGetProperty("finished", out _), Is.True);
        });
    }

    [Test]
    public async Task TurnirDetaljiTest()
    {
        await using var response = await _request.GetAsync("/api/tournaments/t_bsk_1?topN=10&chatN=50");

        if (response.Status != 200)
        {
            Assert.Fail($"Code: {response.Status} - {response.StatusText}");
        }

        var root = await ReadJson(response);

        Assert.Multiple(() =>
        {
            Assert.That(root.GetProperty("tournamentId").GetString(), Is.EqualTo("t_bsk_1"));
            Assert.That(root.GetProperty("sport").GetString(), Is.EqualTo("Basketball"));
            Assert.That(root.GetProperty("hosts").GetArrayLength(), Is.GreaterThan(0));
        });
    }

    [Test]
    public async Task TurnirNijePronadjenTest()
    {
        await using var response = await _request.GetAsync("/api/tournaments/t_ne_postoji");
        Assert.That(response.Status, Is.EqualTo(404));
    }

    [Test]
    public async Task AdminKorisniciTest()
    {
        var token = await PrijaviSeVratiToken("admin01", "admin123");

        await using var response = await _request.GetAsync("/api/admin/users", new()
        {
            Headers = AuthHeaders(token)
        });

        if (response.Status != 200)
        {
            Assert.Fail($"Code: {response.Status} - {response.StatusText}");
        }

        var root = await ReadJson(response);
        Assert.That(root.GetArrayLength(), Is.GreaterThan(0));
    }

    [Test]
    public async Task AdminPromenaRoleTest()
    {
        var user = await RegistrujNovogViewer("role");
        var adminToken = await PrijaviSeVratiToken("admin01", "admin123");

        await using var setRole = await _request.PostAsync($"/api/admin/users/{user.Username}/role", new()
        {
            DataObject = new { role = "Host" },
            Headers = AuthHeaders(adminToken)
        });

        if (setRole.Status != 200)
        {
            Assert.Fail($"SetRole pao ({setRole.Status}) {setRole.StatusText}");
        }

        var noviToken = await PrijaviSeVratiToken(user.Username, user.Password);
        await using var me = await _request.GetAsync("/api/auth/me", new()
        {
            Headers = AuthHeaders(noviToken)
        });

        Assert.That(me.Status, Is.EqualTo(200));
        var root = await ReadJson(me);
        Assert.That(root.GetProperty("role").GetString(), Is.EqualTo("Host"));
    }

    [Test]
    public async Task ViewerNeMozeAdminEndpointTest()
    {
        var viewerToken = await PrijaviSeVratiToken("viewer01", "view123");

        await using var response = await _request.GetAsync("/api/admin/users", new()
        {
            Headers = AuthHeaders(viewerToken)
        });

        Assert.That(response.Status, Is.EqualTo(403).Or.EqualTo(401));
    }

    [Test]
    public async Task ViewerCreateTeamIMojiTimoviTest()
    {
        var user = await RegistrujNovogViewer("team");
        var team = await KreirajTimKaoViewer(user.Token, "Chess");

        await using var response = await _request.GetAsync("/api/teams/my-teams", new()
        {
            Headers = AuthHeaders(user.Token)
        });

        Assert.That(response.Status, Is.EqualTo(200));

        var root = await ReadJson(response);
        var ids = root.EnumerateArray()
            .Select(x => x.GetProperty("teamId").GetString())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToList();

        Assert.That(ids, Contains.Item(team.TeamId));
    }

    [Test]
    public async Task WorkflowApplyApproveTest()
    {
        var hostToken = await PrijaviSeVratiToken("host01", "host123");
        var turnir = await KreirajTurnirKaoHost(hostToken, "Chess");
        var viewer = await RegistrujNovogViewer("appr");
        var team = await KreirajTimKaoViewer(viewer.Token, "Chess");

        await using var apply = await _request.PostAsync($"/api/teams/{team.TeamId}/apply/{turnir.TournamentId}", new()
        {
            Headers = AuthHeaders(viewer.Token)
        });
        Assert.That(apply.Status, Is.EqualTo(200));

        await using var apps = await _request.GetAsync($"/api/teams/applications/{turnir.TournamentId}?status=Pending", new()
        {
            Headers = AuthHeaders(hostToken)
        });
        Assert.That(apps.Status, Is.EqualTo(200));

        var pending = await ReadJson(apps);
        var pendingIds = pending.EnumerateArray().Select(x => x.GetProperty("teamId").GetString()).ToList();
        Assert.That(pendingIds, Contains.Item(team.TeamId));

        await using var approve = await _request.PostAsync($"/api/teams/applications/{turnir.TournamentId}/{team.TeamId}/approve", new()
        {
            Headers = AuthHeaders(hostToken)
        });
        Assert.That(approve.Status, Is.EqualTo(200));

        await using var tournament = await _request.GetAsync($"/api/tournaments/{turnir.TournamentId}");
        Assert.That(tournament.Status, Is.EqualTo(200));

        var root = await ReadJson(tournament);
        var enteredIds = root.GetProperty("enteredTeams")
            .EnumerateArray()
            .Select(x => x.GetProperty("teamId").GetString())
            .ToList();
        Assert.That(enteredIds, Contains.Item(team.TeamId));
    }

    [Test]
    public async Task WorkflowApplyRejectTest()
    {
        var hostToken = await PrijaviSeVratiToken("host01", "host123");
        var turnir = await KreirajTurnirKaoHost(hostToken, "Football");
        var viewer = await RegistrujNovogViewer("rej");
        var team = await KreirajTimKaoViewer(viewer.Token, "Football");

        await using var apply = await _request.PostAsync($"/api/teams/{team.TeamId}/apply/{turnir.TournamentId}", new()
        {
            Headers = AuthHeaders(viewer.Token)
        });
        Assert.That(apply.Status, Is.EqualTo(200));

        await using var reject = await _request.PostAsync($"/api/teams/applications/{turnir.TournamentId}/{team.TeamId}/reject", new()
        {
            Headers = AuthHeaders(hostToken)
        });
        Assert.That(reject.Status, Is.EqualTo(200));

        await using var rejected = await _request.GetAsync($"/api/teams/applications/{turnir.TournamentId}?status=Rejected", new()
        {
            Headers = AuthHeaders(hostToken)
        });
        Assert.That(rejected.Status, Is.EqualTo(200));

        var root = await ReadJson(rejected);
        var rejectedIds = root.EnumerateArray().Select(x => x.GetProperty("teamId").GetString()).ToList();
        Assert.That(rejectedIds, Contains.Item(team.TeamId));
    }

    [Test]
    public async Task DuplicateApplyConflictTest()
    {
        var hostToken = await PrijaviSeVratiToken("host01", "host123");
        var turnir = await KreirajTurnirKaoHost(hostToken, "Chess");
        var viewer = await RegistrujNovogViewer("dupl");
        var team = await KreirajTimKaoViewer(viewer.Token, "Chess");

        await using var first = await _request.PostAsync($"/api/teams/{team.TeamId}/apply/{turnir.TournamentId}", new()
        {
            Headers = AuthHeaders(viewer.Token)
        });
        Assert.That(first.Status, Is.EqualTo(200));

        await using var second = await _request.PostAsync($"/api/teams/{team.TeamId}/apply/{turnir.TournamentId}", new()
        {
            Headers = AuthHeaders(viewer.Token)
        });
        Assert.That(second.Status, Is.EqualTo(409));
    }

    [Test]
    public async Task WorkflowStartScoreFinishTest()
    {
        var hostToken = await PrijaviSeVratiToken("host01", "host123");
        var turnir = await KreirajTurnirKaoHost(hostToken, "Chess");

        var viewer1 = await RegistrujNovogViewer("st1");
        var viewer2 = await RegistrujNovogViewer("st2");

        var team1 = await KreirajTimKaoViewer(viewer1.Token, "Chess");
        var team2 = await KreirajTimKaoViewer(viewer2.Token, "Chess");

        await using var a1 = await _request.PostAsync($"/api/teams/{team1.TeamId}/apply/{turnir.TournamentId}", new()
        {
            Headers = AuthHeaders(viewer1.Token)
        });
        await using var a2 = await _request.PostAsync($"/api/teams/{team2.TeamId}/apply/{turnir.TournamentId}", new()
        {
            Headers = AuthHeaders(viewer2.Token)
        });
        Assert.Multiple(() =>
        {
            Assert.That(a1.Status, Is.EqualTo(200));
            Assert.That(a2.Status, Is.EqualTo(200));
        });

        await using var ap1 = await _request.PostAsync($"/api/teams/applications/{turnir.TournamentId}/{team1.TeamId}/approve", new()
        {
            Headers = AuthHeaders(hostToken)
        });
        await using var ap2 = await _request.PostAsync($"/api/teams/applications/{turnir.TournamentId}/{team2.TeamId}/approve", new()
        {
            Headers = AuthHeaders(hostToken)
        });
        Assert.Multiple(() =>
        {
            Assert.That(ap1.Status, Is.EqualTo(200));
            Assert.That(ap2.Status, Is.EqualTo(200));
        });

        await using var start = await _request.PostAsync($"/api/tournaments/{turnir.TournamentId}/start", new()
        {
            Headers = AuthHeaders(hostToken)
        });
        Assert.That(start.Status, Is.EqualTo(200));

        await using var score = await _request.PostAsync($"/api/tournaments/{turnir.TournamentId}/score", new()
        {
            Headers = AuthHeaders(hostToken),
            DataObject = new
            {
                teamId = team1.TeamId,
                score = 9
            }
        });
        Assert.That(score.Status, Is.EqualTo(200));

        await using var detailsLive = await _request.GetAsync($"/api/tournaments/{turnir.TournamentId}?topN=20&chatN=10");
        Assert.That(detailsLive.Status, Is.EqualTo(200));

        var liveRoot = await ReadJson(detailsLive);
        Assert.That(liveRoot.GetProperty("status").GetString(), Is.EqualTo("Live"));

        var lbEntry = liveRoot.GetProperty("leaderboard")
            .EnumerateArray()
            .FirstOrDefault(x => x.TryGetProperty("teamId", out var id) && id.GetString() == team1.TeamId);
        Assert.That(lbEntry.ValueKind, Is.EqualTo(JsonValueKind.Object));
        Assert.That(lbEntry.GetProperty("score").GetDouble(), Is.EqualTo(9));

        await using var finish = await _request.PostAsync($"/api/tournaments/{turnir.TournamentId}/finish", new()
        {
            Headers = AuthHeaders(hostToken)
        });
        Assert.That(finish.Status, Is.EqualTo(200));

        await using var detailsFinished = await _request.GetAsync($"/api/tournaments/{turnir.TournamentId}");
        Assert.That(detailsFinished.Status, Is.EqualTo(200));

        var finishedRoot = await ReadJson(detailsFinished);
        Assert.That(finishedRoot.GetProperty("status").GetString(), Is.EqualTo("Finished"));
    }

    [Test]
    public async Task GlobalniChatReadWriteTest()
    {
        var token = await PrijaviSeVratiToken("viewer01", "view123");
        var text = $"pw_global_{Guid.NewGuid():N}".Substring(0, 18);

        await using var send = await _request.PostAsync("/api/chat/global", new()
        {
            Headers = AuthHeaders(token),
            DataObject = new { text }
        });
        Assert.That(send.Status, Is.EqualTo(200));

        await using var response = await _request.GetAsync("/api/chat/global?last=50");
        Assert.That(response.Status, Is.EqualTo(200));

        var root = await ReadJson(response);
        var texts = root.EnumerateArray()
            .Select(x => x.GetProperty("text").GetString())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToList();
        Assert.That(texts, Contains.Item(text));
    }

    [Test]
    public async Task TournamentChatReadWriteTest()
    {
        var token = await PrijaviSeVratiToken("viewer02", "view123");
        var text = $"pw_t_{Guid.NewGuid():N}".Substring(0, 16);

        await using var send = await _request.PostAsync("/api/chat/tournament/t_bsk_1", new()
        {
            Headers = AuthHeaders(token),
            DataObject = new { text }
        });
        Assert.That(send.Status, Is.EqualTo(200));

        await using var response = await _request.GetAsync("/api/chat/tournament/t_bsk_1?last=50");
        Assert.That(response.Status, Is.EqualTo(200));

        var root = await ReadJson(response);
        var texts = root.EnumerateArray()
            .Select(x => x.GetProperty("text").GetString())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToList();
        Assert.That(texts, Contains.Item(text));
    }

    [Test]
    public async Task RegistracijaINoviLoginTest()
    {
        var user = await RegistrujNovogViewer("pw");
        var token = await PrijaviSeVratiToken(user.Username, user.Password);
        Assert.That(token.Length, Is.GreaterThan(10));
    }
}
