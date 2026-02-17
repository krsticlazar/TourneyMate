using System.Reflection;
using Api.Tests.TestUtils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TourneyMate.Api.Controllers;

namespace Api.Tests;

[TestFixture]
public class TeamControllerTests
{
    private TeamController _controller = null!;

    [SetUp]
    public void Setup()
    {
        _controller = new TeamController(null!);
    }

    [TearDown]
    public void CleanUp()
    {
        _controller = null!;
    }

    [Test]
    public async Task CreateTeam_VracaBadRequest_KadFaliNaziv()
    {
        TestPomocnik.Ulogovan(_controller, "krle");
        var result = await _controller.CreateTeam(new TeamController.CreateTeamRequest("", "Football"));

        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task CreateTeam_VracaBadRequest_KadJeNazivWhitespace()
    {
        TestPomocnik.Ulogovan(_controller, "krle");
        var result = await _controller.CreateTeam(new TeamController.CreateTeamRequest("   ", "Football"));

        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task CreateTeam_VracaBadRequest_KadFaliSport()
    {
        TestPomocnik.Ulogovan(_controller, "krle");
        var result = await _controller.CreateTeam(new TeamController.CreateTeamRequest("Team X", ""));

        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task CreateTeam_VracaUnauthorized_KadNijeUlogovan()
    {
        TestPomocnik.NijeUlogovan(_controller);
        var result = await _controller.CreateTeam(new TeamController.CreateTeamRequest("Team X", "Football"));

        Assert.That(result, Is.InstanceOf<UnauthorizedObjectResult>());
    }

    [Test]
    public void CreateTeam_ImaAuthorizeViewerIRoute()
    {
        var method = typeof(TeamController).GetMethod(nameof(TeamController.CreateTeam));
        var auth = method?.GetCustomAttribute<AuthorizeAttribute>();
        var post = method?.GetCustomAttribute<HttpPostAttribute>();

        Assert.That(auth, Is.Not.Null);
        Assert.That(auth!.Roles, Is.EqualTo("Viewer"));
        Assert.That(post, Is.Not.Null);
        Assert.That(post!.Template, Is.Null);
    }

    [Test]
    public void CreateTeam_VracaTaskIActionResult()
    {
        var method = typeof(TeamController).GetMethod(nameof(TeamController.CreateTeam));

        Assert.That(method, Is.Not.Null);
        Assert.That(method!.ReturnType, Is.EqualTo(typeof(Task<IActionResult>)));
    }

    [Test]
    public async Task ApplyForTournament_VracaBadRequest_KadFaliTeamId()
    {
        TestPomocnik.Ulogovan(_controller, "krle");
        var result = await _controller.ApplyForTournament("", "tour1");

        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task ApplyForTournament_VracaBadRequest_KadFaliTournamentId()
    {
        TestPomocnik.Ulogovan(_controller, "krle");
        var result = await _controller.ApplyForTournament("team1", "");

        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task ApplyForTournament_VracaUnauthorized_KadNijeUlogovan()
    {
        TestPomocnik.NijeUlogovan(_controller);
        var result = await _controller.ApplyForTournament("team1", "tour1");

        Assert.That(result, Is.InstanceOf<UnauthorizedObjectResult>());
    }

    [Test]
    public void ApplyForTournament_ImaAuthorizeViewerIRoute()
    {
        var method = typeof(TeamController).GetMethod(nameof(TeamController.ApplyForTournament));
        var auth = method?.GetCustomAttribute<AuthorizeAttribute>();
        var post = method?.GetCustomAttribute<HttpPostAttribute>();

        Assert.That(auth, Is.Not.Null);
        Assert.That(auth!.Roles, Is.EqualTo("Viewer"));
        Assert.That(post, Is.Not.Null);
        Assert.That(post!.Template, Is.EqualTo("{teamId}/apply/{tournamentId}"));
    }

    [Test]
    public void ApplyForTournament_VracaTaskIActionResult()
    {
        var method = typeof(TeamController).GetMethod(nameof(TeamController.ApplyForTournament));

        Assert.That(method, Is.Not.Null);
        Assert.That(method!.ReturnType, Is.EqualTo(typeof(Task<IActionResult>)));
    }

    [Test]
    public async Task GetApplications_VracaBadRequest_KadFaliTournamentId()
    {
        TestPomocnik.Ulogovan(_controller, "host", "Host");
        var result = await _controller.GetApplications("", "Pending");

        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task GetApplications_VracaUnauthorized_KadNijeUlogovan()
    {
        TestPomocnik.NijeUlogovan(_controller);
        var result = await _controller.GetApplications("tour1", "Pending");

        Assert.That(result, Is.InstanceOf<UnauthorizedObjectResult>());
    }

    [Test]
    public void GetApplications_ImaAuthorizeHostIRoute()
    {
        var method = typeof(TeamController).GetMethod(nameof(TeamController.GetApplications));
        var auth = method?.GetCustomAttribute<AuthorizeAttribute>();
        var get = method?.GetCustomAttribute<HttpGetAttribute>();

        Assert.That(auth, Is.Not.Null);
        Assert.That(auth!.Roles, Is.EqualTo("Host"));
        Assert.That(get, Is.Not.Null);
        Assert.That(get!.Template, Is.EqualTo("applications/{tournamentId}"));
    }

    [Test]
    public async Task ApproveApplication_VracaBadRequest_KadFaliTournamentId()
    {
        TestPomocnik.Ulogovan(_controller, "host", "Host");
        var result = await _controller.ApproveApplication("", "team1");

        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task ApproveApplication_VracaBadRequest_KadFaliTeamId()
    {
        TestPomocnik.Ulogovan(_controller, "host", "Host");
        var result = await _controller.ApproveApplication("tour1", "");

        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task ApproveApplication_VracaUnauthorized_KadNijeUlogovan()
    {
        TestPomocnik.NijeUlogovan(_controller);
        var result = await _controller.ApproveApplication("tour1", "team1");

        Assert.That(result, Is.InstanceOf<UnauthorizedObjectResult>());
    }

    [Test]
    public void ApproveApplication_ImaAuthorizeHostIRoute()
    {
        var method = typeof(TeamController).GetMethod(nameof(TeamController.ApproveApplication));
        var auth = method?.GetCustomAttribute<AuthorizeAttribute>();
        var post = method?.GetCustomAttribute<HttpPostAttribute>();

        Assert.That(auth, Is.Not.Null);
        Assert.That(auth!.Roles, Is.EqualTo("Host"));
        Assert.That(post, Is.Not.Null);
        Assert.That(post!.Template, Is.EqualTo("applications/{tournamentId}/{teamId}/approve"));
    }

    [Test]
    public void ApproveApplication_VracaTaskIActionResult()
    {
        var method = typeof(TeamController).GetMethod(nameof(TeamController.ApproveApplication));

        Assert.That(method, Is.Not.Null);
        Assert.That(method!.ReturnType, Is.EqualTo(typeof(Task<IActionResult>)));
    }

    [Test]
    public async Task RejectApplication_VracaBadRequest_KadFaliTournamentId()
    {
        TestPomocnik.Ulogovan(_controller, "host", "Host");
        var result = await _controller.RejectApplication("", "team1");

        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task RejectApplication_VracaBadRequest_KadFaliTeamId()
    {
        TestPomocnik.Ulogovan(_controller, "host", "Host");
        var result = await _controller.RejectApplication("tour1", "");

        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task RejectApplication_VracaUnauthorized_KadNijeUlogovan()
    {
        TestPomocnik.NijeUlogovan(_controller);
        var result = await _controller.RejectApplication("tour1", "team1");

        Assert.That(result, Is.InstanceOf<UnauthorizedObjectResult>());
    }

    [Test]
    public void RejectApplication_ImaAuthorizeHostIRoute()
    {
        var method = typeof(TeamController).GetMethod(nameof(TeamController.RejectApplication));
        var auth = method?.GetCustomAttribute<AuthorizeAttribute>();
        var post = method?.GetCustomAttribute<HttpPostAttribute>();

        Assert.That(auth, Is.Not.Null);
        Assert.That(auth!.Roles, Is.EqualTo("Host"));
        Assert.That(post, Is.Not.Null);
        Assert.That(post!.Template, Is.EqualTo("applications/{tournamentId}/{teamId}/reject"));
    }

    [Test]
    public void RejectApplication_VracaTaskIActionResult()
    {
        var method = typeof(TeamController).GetMethod(nameof(TeamController.RejectApplication));

        Assert.That(method, Is.Not.Null);
        Assert.That(method!.ReturnType, Is.EqualTo(typeof(Task<IActionResult>)));
    }

    [Test]
    public async Task GetMyTeams_VracaUnauthorized_KadNijeUlogovan()
    {
        TestPomocnik.NijeUlogovan(_controller);
        var result = await _controller.GetMyTeams();

        Assert.That(result, Is.InstanceOf<UnauthorizedObjectResult>());
    }

    [Test]
    public void GetMyTeams_ImaAuthorizeViewerIRoute()
    {
        var method = typeof(TeamController).GetMethod(nameof(TeamController.GetMyTeams));
        var auth = method?.GetCustomAttribute<AuthorizeAttribute>();
        var get = method?.GetCustomAttribute<HttpGetAttribute>();

        Assert.That(auth, Is.Not.Null);
        Assert.That(auth!.Roles, Is.EqualTo("Viewer"));
        Assert.That(get, Is.Not.Null);
        Assert.That(get!.Template, Is.EqualTo("my-teams"));
    }

    [Test]
    public void GetMyTeams_VracaTaskIActionResult()
    {
        var method = typeof(TeamController).GetMethod(nameof(TeamController.GetMyTeams));

        Assert.That(method, Is.Not.Null);
        Assert.That(method!.ReturnType, Is.EqualTo(typeof(Task<IActionResult>)));
    }

    [Test]
    public void TeamKontroler_ImaRouteApiTeams()
    {
        var route = typeof(TeamController).GetCustomAttribute<RouteAttribute>();

        Assert.That(route, Is.Not.Null);
        Assert.That(route!.Template, Is.EqualTo("api/teams"));
    }
}
