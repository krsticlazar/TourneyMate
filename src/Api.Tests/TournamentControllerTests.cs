using System.Reflection;
using Api.Tests.TestUtils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TourneyMate.Api.Controllers;

namespace Api.Tests;

[TestFixture]
public class TournamentControllerTests
{
    private TournamentController _controller = null!;

    [SetUp]
    public void Setup()
    {
        _controller = new TournamentController(null!, null!, null!);
    }

    [TearDown]
    public void CleanUp()
    {
        _controller = null!;
    }

    [Test]
    public async Task GetTournament_VracaBadRequest_KadFaliId()
    {
        var result = await _controller.GetTournament("", 10, 50);

        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public void GetTournament_ImaRouteId()
    {
        var method = typeof(TournamentController).GetMethod(nameof(TournamentController.GetTournament));
        var get = method?.GetCustomAttribute<HttpGetAttribute>();

        Assert.That(get, Is.Not.Null);
        Assert.That(get!.Template, Is.EqualTo("{id}"));
    }

    [Test]
    public void GetTournament_VracaTaskIActionResult()
    {
        var method = typeof(TournamentController).GetMethod(nameof(TournamentController.GetTournament));

        Assert.That(method, Is.Not.Null);
        Assert.That(method!.ReturnType, Is.EqualTo(typeof(Task<IActionResult>)));
    }

    [Test]
    public void TournamentKontroler_ImaRouteApiTournaments()
    {
        var route = typeof(TournamentController).GetCustomAttribute<RouteAttribute>();

        Assert.That(route, Is.Not.Null);
        Assert.That(route!.Template, Is.EqualTo("api/tournaments"));
    }

    [Test]
    public async Task UpdateScore_VracaBadRequest_KadFaliTournamentId()
    {
        TestPomocnik.Ulogovan(_controller, "host", "Host");
        var result = await _controller.UpdateScore("", new TournamentController.UpdateScoreRequest("team1", 10));

        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task UpdateScore_VracaBadRequest_KadFaliTeamId()
    {
        TestPomocnik.Ulogovan(_controller, "host", "Host");
        var result = await _controller.UpdateScore("tour1", new TournamentController.UpdateScoreRequest("", 10));

        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task UpdateScore_VracaUnauthorized_KadNijeUlogovan()
    {
        TestPomocnik.NijeUlogovan(_controller);
        var result = await _controller.UpdateScore("tour1", new TournamentController.UpdateScoreRequest("team1", 10));

        Assert.That(result, Is.InstanceOf<UnauthorizedObjectResult>());
    }

    [Test]
    public void UpdateScore_ImaAuthorizeHostIRoute()
    {
        var method = typeof(TournamentController).GetMethod(nameof(TournamentController.UpdateScore));
        var auth = method?.GetCustomAttribute<AuthorizeAttribute>();
        var post = method?.GetCustomAttribute<HttpPostAttribute>();

        Assert.That(auth, Is.Not.Null);
        Assert.That(auth!.Roles, Is.EqualTo("Host"));
        Assert.That(post, Is.Not.Null);
        Assert.That(post!.Template, Is.EqualTo("{tournamentId}/score"));
    }

    [Test]
    public void UpdateScore_VracaTaskIActionResult()
    {
        var method = typeof(TournamentController).GetMethod(nameof(TournamentController.UpdateScore));

        Assert.That(method, Is.Not.Null);
        Assert.That(method!.ReturnType, Is.EqualTo(typeof(Task<IActionResult>)));
    }

    [Test]
    public async Task CreateTournament_VracaBadRequest_KadFaliNaziv()
    {
        TestPomocnik.Ulogovan(_controller, "host", "Host");
        var result = await _controller.CreateTournament(new TournamentController.CreateTournamentRequest("", "Football"));

        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task CreateTournament_VracaBadRequest_KadFaliSport()
    {
        TestPomocnik.Ulogovan(_controller, "host", "Host");
        var result = await _controller.CreateTournament(new TournamentController.CreateTournamentRequest("Kup", ""));

        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task CreateTournament_VracaUnauthorized_KadNijeUlogovan()
    {
        TestPomocnik.NijeUlogovan(_controller);
        var result = await _controller.CreateTournament(new TournamentController.CreateTournamentRequest("Kup", "Football"));

        Assert.That(result, Is.InstanceOf<UnauthorizedObjectResult>());
    }

    [Test]
    public void CreateTournament_ImaAuthorizeHostIRoute()
    {
        var method = typeof(TournamentController).GetMethod(nameof(TournamentController.CreateTournament));
        var auth = method?.GetCustomAttribute<AuthorizeAttribute>();
        var post = method?.GetCustomAttribute<HttpPostAttribute>();

        Assert.That(auth, Is.Not.Null);
        Assert.That(auth!.Roles, Is.EqualTo("Host"));
        Assert.That(post, Is.Not.Null);
        Assert.That(post!.Template, Is.EqualTo("create"));
    }

    [Test]
    public void CreateTournament_VracaTaskIActionResult()
    {
        var method = typeof(TournamentController).GetMethod(nameof(TournamentController.CreateTournament));

        Assert.That(method, Is.Not.Null);
        Assert.That(method!.ReturnType, Is.EqualTo(typeof(Task<IActionResult>)));
    }

    [Test]
    public async Task StartTournament_VracaBadRequest_KadFaliTournamentId()
    {
        TestPomocnik.Ulogovan(_controller, "host", "Host");
        var result = await _controller.StartTournament("");

        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task StartTournament_VracaUnauthorized_KadNijeUlogovan()
    {
        TestPomocnik.NijeUlogovan(_controller);
        var result = await _controller.StartTournament("tour1");

        Assert.That(result, Is.InstanceOf<UnauthorizedObjectResult>());
    }

    [Test]
    public void StartTournament_ImaAuthorizeHostIRoute()
    {
        var method = typeof(TournamentController).GetMethod(nameof(TournamentController.StartTournament));
        var auth = method?.GetCustomAttribute<AuthorizeAttribute>();
        var post = method?.GetCustomAttribute<HttpPostAttribute>();

        Assert.That(auth, Is.Not.Null);
        Assert.That(auth!.Roles, Is.EqualTo("Host"));
        Assert.That(post, Is.Not.Null);
        Assert.That(post!.Template, Is.EqualTo("{tournamentId}/start"));
    }

    [Test]
    public void StartTournament_VracaTaskIActionResult()
    {
        var method = typeof(TournamentController).GetMethod(nameof(TournamentController.StartTournament));

        Assert.That(method, Is.Not.Null);
        Assert.That(method!.ReturnType, Is.EqualTo(typeof(Task<IActionResult>)));
    }

    [Test]
    public async Task FinishTournament_VracaBadRequest_KadFaliTournamentId()
    {
        TestPomocnik.Ulogovan(_controller, "host", "Host");
        var result = await _controller.FinishTournament("");

        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task FinishTournament_VracaUnauthorized_KadNijeUlogovan()
    {
        TestPomocnik.NijeUlogovan(_controller);
        var result = await _controller.FinishTournament("tour1");

        Assert.That(result, Is.InstanceOf<UnauthorizedObjectResult>());
    }

    [Test]
    public void FinishTournament_ImaAuthorizeHostIRoute()
    {
        var method = typeof(TournamentController).GetMethod(nameof(TournamentController.FinishTournament));
        var auth = method?.GetCustomAttribute<AuthorizeAttribute>();
        var post = method?.GetCustomAttribute<HttpPostAttribute>();

        Assert.That(auth, Is.Not.Null);
        Assert.That(auth!.Roles, Is.EqualTo("Host"));
        Assert.That(post, Is.Not.Null);
        Assert.That(post!.Template, Is.EqualTo("{tournamentId}/finish"));
    }

    [Test]
    public void FinishTournament_VracaTaskIActionResult()
    {
        var method = typeof(TournamentController).GetMethod(nameof(TournamentController.FinishTournament));

        Assert.That(method, Is.Not.Null);
        Assert.That(method!.ReturnType, Is.EqualTo(typeof(Task<IActionResult>)));
    }
}
