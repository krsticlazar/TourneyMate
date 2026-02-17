namespace Web.Tests;

[Parallelizable(ParallelScope.Self)]
[TestFixture]
public class StranicaTest : PageTest
{
    private const string WebUrl = "http://localhost:5173/";
    private const string ApiUrl = "http://localhost:5125";

    public override BrowserNewContextOptions ContextOptions()
    {
        return new BrowserNewContextOptions
        {
            ViewportSize = new ViewportSize
            {
                Width = 1280,
                Height = 720
            }
        };
    }

    private static string UniqueId(string prefix, int len = 12)
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

    private async Task<string> ApiLoginToken(string username, string password)
    {
        await using var api = await Playwright.APIRequest.NewContextAsync(new()
        {
            BaseURL = ApiUrl,
            ExtraHTTPHeaders = new Dictionary<string, string> { { "Accept", "application/json" } }
        });

        await using var response = await api.PostAsync("/api/auth/login", new()
        {
            DataObject = new { username, password }
        });

        if (response.Status != 200)
        {
            Assert.Fail($"Api login neuspesan ({response.Status}) {response.StatusText}");
        }

        var root = await ReadJson(response);
        var token = root.GetProperty("token").GetString();
        Assert.That(token, Is.Not.Null.And.Not.Empty);
        return token!;
    }

    private async Task<(string Username, string Password, string DisplayName)> ApiRegistrujViewer(string prefix = "uiv")
    {
        var username = UniqueId(prefix, 14);
        var password = "test123";
        var displayName = $"UI_{username}";

        await using var api = await Playwright.APIRequest.NewContextAsync(new()
        {
            BaseURL = ApiUrl,
            ExtraHTTPHeaders = new Dictionary<string, string> { { "Accept", "application/json" } }
        });

        await using var response = await api.PostAsync("/api/auth/register", new()
        {
            DataObject = new
            {
                username,
                password,
                displayName
            }
        });

        if (response.Status != 200)
        {
            Assert.Fail($"Api register neuspesan ({response.Status}) {response.StatusText}");
        }

        return (username, password, displayName);
    }

    private async Task<(string TournamentId, string TournamentName)> ApiKreirajTurnirKaoHost(string hostToken, string sport = "Chess")
    {
        var tournamentName = $"PW_{sport}_{Guid.NewGuid():N}".Substring(0, 18);

        await using var api = await Playwright.APIRequest.NewContextAsync(new()
        {
            BaseURL = ApiUrl,
            ExtraHTTPHeaders = new Dictionary<string, string>
            {
                { "Accept", "application/json" },
                { "Authorization", $"Bearer {hostToken}" }
            }
        });

        await using var response = await api.PostAsync("/api/tournaments/create", new()
        {
            DataObject = new
            {
                name = tournamentName,
                sport
            }
        });

        if (response.Status != 200)
        {
            Assert.Fail($"Api create tournament neuspesan ({response.Status}) {response.StatusText}");
        }

        var root = await ReadJson(response);
        var tournamentId = root.GetProperty("tournamentId").GetString();
        Assert.That(tournamentId, Is.Not.Null.And.Not.Empty);
        return (tournamentId!, tournamentName);
    }

    private async Task<(string TeamId, string TeamName)> ApiKreirajTimKaoViewer(string viewerToken, string sport = "Chess")
    {
        var teamName = $"PWTeam_{Guid.NewGuid():N}".Substring(0, 16);

        await using var api = await Playwright.APIRequest.NewContextAsync(new()
        {
            BaseURL = ApiUrl,
            ExtraHTTPHeaders = new Dictionary<string, string>
            {
                { "Accept", "application/json" },
                { "Authorization", $"Bearer {viewerToken}" }
            }
        });

        await using var response = await api.PostAsync("/api/teams", new()
        {
            DataObject = new
            {
                name = teamName,
                sport
            }
        });

        if (response.Status != 200)
        {
            Assert.Fail($"Api create team neuspesan ({response.Status}) {response.StatusText}");
        }

        var root = await ReadJson(response);
        var teamId = root.GetProperty("teamId").GetString();
        Assert.That(teamId, Is.Not.Null.And.Not.Empty);
        return (teamId!, teamName);
    }

    private async Task ApiPrijaviTimNaTurnir(string viewerToken, string teamId, string tournamentId)
    {
        await using var api = await Playwright.APIRequest.NewContextAsync(new()
        {
            BaseURL = ApiUrl,
            ExtraHTTPHeaders = new Dictionary<string, string>
            {
                { "Accept", "application/json" },
                { "Authorization", $"Bearer {viewerToken}" }
            }
        });

        await using var response = await api.PostAsync($"/api/teams/{teamId}/apply/{tournamentId}");

        if (response.Status != 200)
        {
            Assert.Fail($"Api apply neuspesan ({response.Status}) {response.StatusText}");
        }
    }

    private async Task ApiOdobriPrijavu(string hostToken, string tournamentId, string teamId)
    {
        await using var api = await Playwright.APIRequest.NewContextAsync(new()
        {
            BaseURL = ApiUrl,
            ExtraHTTPHeaders = new Dictionary<string, string>
            {
                { "Accept", "application/json" },
                { "Authorization", $"Bearer {hostToken}" }
            }
        });

        await using var response = await api.PostAsync($"/api/teams/applications/{tournamentId}/{teamId}/approve");

        if (response.Status != 200)
        {
            Assert.Fail($"Api approve neuspesan ({response.Status}) {response.StatusText}");
        }
    }

    private async Task<string> PrihvatiDialog(Func<Task> action)
    {
        var tcs = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);

        async void Handler(object? sender, IDialog dialog)
        {
            try
            {
                var message = dialog.Message;
                await dialog.AcceptAsync();
                tcs.TrySetResult(message);
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
            }
        }

        Page.Dialog += Handler;
        try
        {
            await action();
            var message = await tcs.Task.WaitAsync(TimeSpan.FromSeconds(15));
            return message;
        }
        finally
        {
            Page.Dialog -= Handler;
        }
    }

    private static bool JeApiPoziv(IResponse response, string method, string pathPart)
    {
        return response.Request.Method.Equals(method, StringComparison.OrdinalIgnoreCase)
            && response.Url.Contains(pathPart, StringComparison.OrdinalIgnoreCase);
    }

    private async Task PrijaviSe(string username, string password, string displayName)
    {
        await Page.GotoAsync(WebUrl);

        await Page.GetByRole(AriaRole.Button, new() { Name = "Prijavi se" }).ClickAsync();
        await Page.GetByPlaceholder("Username").FillAsync(username);
        await Page.GetByPlaceholder("Password").FillAsync(password);
        await Page.Locator("form").GetByRole(AriaRole.Button, new() { Name = "Prijavi se" }).ClickAsync();

        await Expect(Page.Locator("header")).ToContainTextAsync(displayName, new() { Timeout = 15000 });
        await Expect(Page.GetByRole(AriaRole.Button, new() { Name = "Logout" })).ToBeVisibleAsync();
    }

    private async Task OdjaviSe()
    {
        await Page.GetByRole(AriaRole.Button, new() { Name = "Logout" }).ClickAsync();
        await Expect(Page.GetByRole(AriaRole.Button, new() { Name = "Prijavi se" })).ToBeVisibleAsync();
    }

    [Test]
    public async Task PocetnaStranaTest()
    {
        await Page.GotoAsync(WebUrl);

        await Expect(Page).ToHaveTitleAsync("tourneymate-web");
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Turniri", Exact = true })).ToBeVisibleAsync();
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Globalni Chat" })).ToBeVisibleAsync();
    }

    [Test]
    public async Task ViewerNavigacijaTest()
    {
        await PrijaviSe("viewer01", "view123", "Viewer_01");

        await Page.GetByRole(AriaRole.Button, new() { Name = "Prijavi Tim" }).ClickAsync();
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Prijavi Tim za Turnir" })).ToBeVisibleAsync();
    }

    [Test]
    public async Task HostNavigacijaTest()
    {
        await PrijaviSe("host01", "host123", "Host_01");

        await Page.GetByRole(AriaRole.Button, new() { Name = "Host Panel" }).ClickAsync();
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Host Panel" })).ToBeVisibleAsync();
    }

    [Test]
    public async Task AdminNavigacijaTest()
    {
        await PrijaviSe("admin01", "admin123", "Admin_01");

        await Page.GetByRole(AriaRole.Button, new() { Name = "Admin Panel" }).ClickAsync();
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Admin Panel" })).ToBeVisibleAsync();
    }

    [Test]
    public async Task OtvaranjeTurniraTest()
    {
        await Page.GotoAsync(WebUrl);

        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Nis_5v5_Cup", Exact = true }).First).ToBeVisibleAsync(new()
        {
            Timeout = 15000
        });
        await Page.GetByRole(AriaRole.Heading, new() { Name = "Nis_5v5_Cup", Exact = true }).First.ClickAsync();
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Nis_5v5_Cup", Exact = true })).ToBeVisibleAsync();
        await Expect(Page.GetByRole(AriaRole.Button, new() { NameRegex = new Regex("Nazad", RegexOptions.IgnoreCase) })).ToBeVisibleAsync();
    }

    [Test]
    public async Task SlanjeGlobalnePorukeTest()
    {
        var text = $"pw_poruka_{Guid.NewGuid():N}".Substring(0, 18);
        await PrijaviSe("viewer02", "view123", "Viewer_02");

        var input = Page.GetByPlaceholder("Unesite poruku...");
        await input.FillAsync(text);
        await input.PressAsync("Enter");

        await Expect(Page.GetByText(text).First).ToBeVisibleAsync();
    }

    [Test]
    public async Task ViewerKreiraTimPrekoUI()
    {
        var viewer = await ApiRegistrujViewer("uitim");
        var teamName = $"UITim_{Guid.NewGuid():N}".Substring(0, 16);

        await PrijaviSe(viewer.Username, viewer.Password, viewer.DisplayName);
        await Page.GetByRole(AriaRole.Button, new() { Name = "Prijavi Tim" }).ClickAsync();

        await Page.GetByRole(AriaRole.Button, new() { NameRegex = new Regex("Napravi Tim", RegexOptions.IgnoreCase) }).ClickAsync();
        await Page.GetByPlaceholder("Ime tima").FillAsync(teamName);
        await Page.Locator("form").GetByRole(AriaRole.Combobox).SelectOptionAsync("Chess");

        await PrihvatiDialog(async () =>
        {
            await Page.Locator("form").GetByRole(AriaRole.Button, new() { NameRegex = new Regex("^Kreiraj$", RegexOptions.IgnoreCase) }).ClickAsync();
        });

        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = teamName, Exact = true })).ToBeVisibleAsync(new() { Timeout = 15000 });
    }

    [Test]
    public async Task HostKreiraTurnirPrekoUI()
    {
        var tournamentName = $"UITurnir_{Guid.NewGuid():N}".Substring(0, 18);

        await PrijaviSe("host01", "host123", "Host_01");
        await Page.GetByRole(AriaRole.Button, new() { Name = "Host Panel" }).ClickAsync();

        await Page.GetByRole(AriaRole.Button, new() { NameRegex = new Regex("Kreiraj Novi Turnir", RegexOptions.IgnoreCase) }).ClickAsync();
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Kreiraj Novi Turnir" })).ToBeVisibleAsync();

        await Page.GetByPlaceholder("npr. Zimska Liga 2026").FillAsync(tournamentName);
        await Page.GetByRole(AriaRole.Combobox).SelectOptionAsync("Chess");

        await PrihvatiDialog(async () =>
        {
            await Page.GetByRole(AriaRole.Button, new() { NameRegex = new Regex("Kreiraj Turnir", RegexOptions.IgnoreCase) }).ClickAsync();
        });

        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Host Panel" })).ToBeVisibleAsync();
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = tournamentName, Exact = true })).ToBeVisibleAsync(new() { Timeout = 15000 });
    }

    [Test]
    public async Task AdminMenjaRoluPrekoUI()
    {
        var user = await ApiRegistrujViewer("uirole");

        await PrijaviSe("admin01", "admin123", "Admin_01");
        await Page.GetByRole(AriaRole.Button, new() { Name = "Admin Panel" }).ClickAsync();

        var row = Page.GetByRole(AriaRole.Row).Filter(new() { HasText = user.Username }).First;
        await Expect(row).ToBeVisibleAsync(new() { Timeout = 15000 });

        await row.GetByRole(AriaRole.Button, new() { NameRegex = new Regex("Promeni", RegexOptions.IgnoreCase) }).ClickAsync();
        await row.GetByRole(AriaRole.Combobox).SelectOptionAsync("Host");

        await PrihvatiDialog(async () =>
        {
            await row.GetByRole(AriaRole.Button, new() { NameRegex = new Regex("Sacuvaj|Sa", RegexOptions.IgnoreCase) }).ClickAsync();
        });

        var updatedRow = Page.GetByRole(AriaRole.Row).Filter(new() { HasText = user.Username }).First;
        await Expect(updatedRow).ToContainTextAsync("Host", new() { Timeout = 15000 });
    }

    [Test]
    public async Task KompletanFlowHostViewerOdobravanjePrekoUI()
    {
        var tournamentName = $"Flow_{Guid.NewGuid():N}".Substring(0, 16);
        var teamName = $"FlowTeam_{Guid.NewGuid():N}".Substring(0, 16);
        var viewer = await ApiRegistrujViewer("uiflow");

        await PrijaviSe("host01", "host123", "Host_01");
        await Page.GetByRole(AriaRole.Button, new() { Name = "Host Panel" }).ClickAsync();

        await Page.GetByRole(AriaRole.Button, new() { NameRegex = new Regex("Kreiraj Novi Turnir", RegexOptions.IgnoreCase) }).ClickAsync();
        await Page.GetByPlaceholder("npr. Zimska Liga 2026").FillAsync(tournamentName);
        await Page.GetByRole(AriaRole.Combobox).SelectOptionAsync("Chess");

        await PrihvatiDialog(async () =>
        {
            await Page.GetByRole(AriaRole.Button, new() { NameRegex = new Regex("Kreiraj Turnir", RegexOptions.IgnoreCase) }).ClickAsync();
        });

        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = tournamentName, Exact = true })).ToBeVisibleAsync(new() { Timeout = 15000 });
        await OdjaviSe();

        await PrijaviSe(viewer.Username, viewer.Password, viewer.DisplayName);
        await Page.GetByRole(AriaRole.Button, new() { Name = "Prijavi Tim" }).ClickAsync();

        await Page.GetByRole(AriaRole.Button, new() { NameRegex = new Regex("Napravi Tim", RegexOptions.IgnoreCase) }).ClickAsync();
        await Page.GetByPlaceholder("Ime tima").FillAsync(teamName);
        await Page.Locator("form").GetByRole(AriaRole.Combobox).SelectOptionAsync("Chess");

        await PrihvatiDialog(async () =>
        {
            await Page.Locator("form").GetByRole(AriaRole.Button, new() { NameRegex = new Regex("^Kreiraj$", RegexOptions.IgnoreCase) }).ClickAsync();
        });

        await Page.GetByRole(AriaRole.Combobox).Nth(0).SelectOptionAsync("Chess");
        await Expect(Page.GetByRole(AriaRole.Combobox)).ToHaveCountAsync(3, new() { Timeout = 15000 });
        await Page.GetByRole(AriaRole.Combobox).Nth(1).SelectOptionAsync(new SelectOptionValue { Label = teamName });
        await Page.GetByRole(AriaRole.Combobox).Nth(2).SelectOptionAsync(new SelectOptionValue { Label = tournamentName });

        await PrihvatiDialog(async () =>
        {
            await Page.GetByRole(AriaRole.Button, new() { Name = "Prijavi Tim" }).Nth(1).ClickAsync();
        });

        await OdjaviSe();

        await PrijaviSe("host01", "host123", "Host_01");
        await Page.GetByRole(AriaRole.Button, new() { Name = "Host Panel" }).ClickAsync();

        await Page.GetByRole(AriaRole.Heading, new() { Name = tournamentName, Exact = true }).ClickAsync();
        await Expect(Page.GetByText(teamName).First).ToBeVisibleAsync(new() { Timeout = 15000 });

        await PrihvatiDialog(async () =>
        {
            await Page.GetByRole(AriaRole.Button, new() { Name = "Odobri" }).First.ClickAsync();
        });

        await Expect(Page.GetByRole(AriaRole.Button, new() { Name = "Odobri" })).ToHaveCountAsync(0, new() { Timeout = 10000 });
    }

    [Test]
    public async Task HostUpravljanjeTurniromStartScoreFinishUI()
    {
        var hostToken = await ApiLoginToken("host01", "host123");
        var turnir = await ApiKreirajTurnirKaoHost(hostToken, "Chess");

        var viewer1 = await ApiRegistrujViewer("mg1");
        var viewer2 = await ApiRegistrujViewer("mg2");

        var token1 = await ApiLoginToken(viewer1.Username, viewer1.Password);
        var token2 = await ApiLoginToken(viewer2.Username, viewer2.Password);

        var team1 = await ApiKreirajTimKaoViewer(token1, "Chess");
        var team2 = await ApiKreirajTimKaoViewer(token2, "Chess");

        await ApiPrijaviTimNaTurnir(token1, team1.TeamId, turnir.TournamentId);
        await ApiPrijaviTimNaTurnir(token2, team2.TeamId, turnir.TournamentId);

        await ApiOdobriPrijavu(hostToken, turnir.TournamentId, team1.TeamId);
        await ApiOdobriPrijavu(hostToken, turnir.TournamentId, team2.TeamId);

        await PrijaviSe("host01", "host123", "Host_01");
        await Page.GetByRole(AriaRole.Button, new() { Name = "Host Panel" }).ClickAsync();

        var heading = Page.GetByRole(AriaRole.Heading, new() { Name = turnir.TournamentName, Exact = true });
        await Expect(heading).ToBeVisibleAsync(new() { Timeout = 15000 });
        var manageButton = heading.Locator("xpath=../../button");
        await Expect(manageButton).ToBeVisibleAsync(new() { Timeout = 15000 });
        await manageButton.ClickAsync();

        await Expect(Page.GetByRole(AriaRole.Button, new() { NameRegex = new Regex("Start", RegexOptions.IgnoreCase) })).ToBeVisibleAsync();

        await PrihvatiDialog(async () =>
        {
            await Page.GetByRole(AriaRole.Button, new() { NameRegex = new Regex("Start", RegexOptions.IgnoreCase) }).ClickAsync();
        });

        await Expect(Page.GetByText("Live").First).ToBeVisibleAsync(new() { Timeout = 15000 });

        await Page.GetByRole(AriaRole.Combobox).First.SelectOptionAsync(new SelectOptionValue { Label = team1.TeamName });
        await Page.GetByRole(AriaRole.Button, new() { Name = "+1" }).ClickAsync();

        await PrihvatiDialog(async () =>
        {
            await Page.GetByRole(AriaRole.Button, new() { NameRegex = new Regex("Bodove", RegexOptions.IgnoreCase) }).ClickAsync();
        });

        await Expect(Page.GetByRole(AriaRole.Cell, new() { Name = team1.TeamName })).ToBeVisibleAsync(new() { Timeout = 15000 });

        await PrihvatiDialog(async () =>
        {
            await Page.GetByRole(AriaRole.Button, new() { NameRegex = new Regex("Zavr", RegexOptions.IgnoreCase) }).ClickAsync();
        });

        await Expect(Page.GetByText("Finished").First).ToBeVisibleAsync(new() { Timeout = 15000 });
    }

    [Test]
    public async Task SlanjePorukeNaTurnirskomChatuTest()
    {
        var text = $"pw_turnir_{Guid.NewGuid():N}".Substring(0, 18);

        await PrijaviSe("viewer03", "view123", "Viewer_03");

        await Page.GetByRole(AriaRole.Heading, new() { Name = "3x3_Arena", Exact = true }).First.ClickAsync();
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "3x3_Arena", Exact = true })).ToBeVisibleAsync();

        var input = Page.GetByPlaceholder("Unesite poruku...");
        await input.FillAsync(text);
        await input.PressAsync("Enter");

        await Expect(Page.GetByText(text).First).ToBeVisibleAsync(new() { Timeout = 15000 });
    }

    [Test]
    public async Task RegistracijaPrekoUI_AutoLoginIRoleViewer()
    {
        var username = UniqueId("uireg", 14);
        var password = "test123";
        var displayName = $"UI_{username}";

        await Page.GotoAsync(WebUrl);
        await Page.GetByRole(AriaRole.Button, new() { Name = "Prijavi se" }).ClickAsync();
        await Page.GetByText("Registrujte se").ClickAsync();

        await Page.GetByPlaceholder("Username").FillAsync(username);
        await Page.GetByPlaceholder("Display Name").FillAsync(displayName);
        await Page.GetByPlaceholder("Password").FillAsync(password);

        var waitRegister = Page.WaitForResponseAsync(r => JeApiPoziv(r, "POST", "/api/auth/register"));
        var waitLogin = Page.WaitForResponseAsync(r => JeApiPoziv(r, "POST", "/api/auth/login"));

        await Page.Locator("form").GetByRole(AriaRole.Button, new() { Name = "Registruj se" }).ClickAsync();

        var registerResponse = await waitRegister;
        var loginResponse = await waitLogin;

        Assert.Multiple(() =>
        {
            Assert.That(registerResponse.Ok, Is.True);
            Assert.That(loginResponse.Ok, Is.True);
        });

        await Expect(Page.Locator("header")).ToContainTextAsync(displayName, new() { Timeout = 15000 });
        await Expect(Page.GetByRole(AriaRole.Button, new() { Name = "Prijavi Tim" })).ToBeVisibleAsync();
    }

    [Test]
    public async Task LogoutPrekoUI_PogadjaApiLogout()
    {
        await PrijaviSe("viewer01", "view123", "Viewer_01");

        var logoutResponse = await Page.RunAndWaitForResponseAsync(async () =>
        {
            await Page.GetByRole(AriaRole.Button, new() { Name = "Logout" }).ClickAsync();
        }, r => JeApiPoziv(r, "POST", "/api/auth/logout"));

        Assert.That(logoutResponse.Ok, Is.True);
        await Expect(Page.GetByRole(AriaRole.Button, new() { Name = "Prijavi se" })).ToBeVisibleAsync();
    }

    [Test]
    public async Task GostNaPocetnojVidiDaMoraPrijavaZaGlobalniChat()
    {
        await Page.GotoAsync(WebUrl);

        await Expect(Page.GetByText("Prijavite se da biste pisali poruke").First).ToBeVisibleAsync();
        await Expect(Page.GetByPlaceholder("Unesite poruku...")).ToHaveCountAsync(0);
    }

    [Test]
    public async Task GostNaTurniruVidiDaMoraPrijavaZaTurnirskiChat()
    {
        await Page.GotoAsync(WebUrl);
        await Page.GetByRole(AriaRole.Heading, new() { Name = "3x3_Arena", Exact = true }).First.ClickAsync();

        await Expect(Page.GetByText("Prijavite se da biste pisali poruke").First).ToBeVisibleAsync();
        await Expect(Page.GetByPlaceholder("Unesite poruku...")).ToHaveCountAsync(0);
    }

    [Test]
    public async Task AdminPanelOtvaranje_PogadjaApiUsers()
    {
        await PrijaviSe("admin01", "admin123", "Admin_01");

        var usersResponse = await Page.RunAndWaitForResponseAsync(async () =>
        {
            await Page.GetByRole(AriaRole.Button, new() { Name = "Admin Panel" }).ClickAsync();
        }, r => JeApiPoziv(r, "GET", "/api/admin/users"));

        Assert.That(usersResponse.Ok, Is.True);
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Admin Panel" })).ToBeVisibleAsync();
    }

    [Test]
    public async Task ApplyStranicaOtvaranje_PogadjaApiMyTeamsIHome()
    {
        var viewer = await ApiRegistrujViewer("uimy");
        await PrijaviSe(viewer.Username, viewer.Password, viewer.DisplayName);

        var waitTeams = Page.WaitForResponseAsync(r => JeApiPoziv(r, "GET", "/api/teams/my-teams"));
        var waitHome = Page.WaitForResponseAsync(r => JeApiPoziv(r, "GET", "/api/home"));

        await Page.GetByRole(AriaRole.Button, new() { Name = "Prijavi Tim" }).ClickAsync();

        var teamsResponse = await waitTeams;
        var homeResponse = await waitHome;

        Assert.Multiple(() =>
        {
            Assert.That(teamsResponse.Ok, Is.True);
            Assert.That(homeResponse.Ok, Is.True);
        });

        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Prijavi Tim za Turnir" })).ToBeVisibleAsync();
    }

    [Test]
    public async Task OtvaranjeTurniraSaHome_PogadjaApiTournamentDetalje()
    {
        await Page.GotoAsync(WebUrl);

        var detailsResponse = await Page.RunAndWaitForResponseAsync(async () =>
        {
            await Page.GetByRole(AriaRole.Heading, new() { Name = "Nis_5v5_Cup", Exact = true }).First.ClickAsync();
        }, r => JeApiPoziv(r, "GET", "/api/tournaments/"));

        Assert.That(detailsResponse.Ok, Is.True);
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Nis_5v5_Cup", Exact = true })).ToBeVisibleAsync();
    }

    [Test]
    public async Task HostOdbijaPrijavuPrekoUI()
    {
        var hostToken = await ApiLoginToken("host01", "host123");
        var turnir = await ApiKreirajTurnirKaoHost(hostToken, "Chess");

        var viewer = await ApiRegistrujViewer("uirej");
        var viewerToken = await ApiLoginToken(viewer.Username, viewer.Password);
        var team = await ApiKreirajTimKaoViewer(viewerToken, "Chess");
        await ApiPrijaviTimNaTurnir(viewerToken, team.TeamId, turnir.TournamentId);

        await PrijaviSe("host01", "host123", "Host_01");
        await Page.GetByRole(AriaRole.Button, new() { Name = "Host Panel" }).ClickAsync();

        var row = Page.Locator("div").Filter(new()
        {
            Has = Page.GetByRole(AriaRole.Heading, new() { Name = turnir.TournamentName, Exact = true })
        }).First;

        await row.GetByRole(AriaRole.Heading, new() { Name = turnir.TournamentName, Exact = true }).ClickAsync();
        await Expect(Page.GetByText(team.TeamName).First).ToBeVisibleAsync(new() { Timeout = 15000 });

        var rejectResponse = await Page.RunAndWaitForResponseAsync(async () =>
        {
            await PrihvatiDialog(async () =>
            {
                await Page.GetByRole(AriaRole.Button, new() { Name = "Odbij" }).First.ClickAsync();
            });
        }, r => JeApiPoziv(r, "POST", $"/api/teams/applications/{turnir.TournamentId}/{team.TeamId}/reject"));

        Assert.That(rejectResponse.Ok, Is.True);
        await Expect(Page.GetByText("Nema pending aplikacija")).ToBeVisibleAsync(new() { Timeout = 15000 });
    }
}
