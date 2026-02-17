using System.Reflection;
using TourneyMate.Api.Dtos;
using Api.Tests.TestUtils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TourneyMate.Api.Controllers;

namespace Api.Tests;

[TestFixture]
public class ChatControllerTests
{
    private ChatController _controller = null!;

    [SetUp]
    public void Setup()
    {
        _controller = new ChatController(null!);
    }

    [TearDown]
    public void CleanUp()
    {
        _controller = null!;
    }

    [Test]
    public async Task SendGlobal_VracaBadRequest_KadFaliPoruka()
    {
        TestPomocnik.Ulogovan(_controller, "krle");
        var result = await _controller.SendGlobal(new ApiDtos.SendChatMessageDto(""));

        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task SendGlobal_VracaBadRequest_KadJePorukaSamoWhitespace()
    {
        TestPomocnik.Ulogovan(_controller, "krle");
        var result = await _controller.SendGlobal(new ApiDtos.SendChatMessageDto("   "));

        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task SendGlobal_VracaUnauthorized_KadNijeUlogovan()
    {
        TestPomocnik.NijeUlogovan(_controller);
        var result = await _controller.SendGlobal(new ApiDtos.SendChatMessageDto("cao"));

        Assert.That(result, Is.InstanceOf<UnauthorizedObjectResult>());
    }

    [Test]
    public void SendGlobal_ImaAuthorizeIRoute()
    {
        var method = typeof(ChatController).GetMethod(nameof(ChatController.SendGlobal));
        var auth = method?.GetCustomAttribute<AuthorizeAttribute>();
        var post = method?.GetCustomAttribute<HttpPostAttribute>();

        Assert.That(auth, Is.Not.Null);
        Assert.That(post, Is.Not.Null);
        Assert.That(post!.Template, Is.EqualTo("global"));
    }

    [Test]
    public void ChatKontroler_ImaRouteApiChat()
    {
        var route = typeof(ChatController).GetCustomAttribute<RouteAttribute>();

        Assert.That(route, Is.Not.Null);
        Assert.That(route!.Template, Is.EqualTo("api/chat"));
    }

    [Test]
    public void SendGlobal_VracaTaskIActionResult()
    {
        var method = typeof(ChatController).GetMethod(nameof(ChatController.SendGlobal));

        Assert.That(method, Is.Not.Null);
        Assert.That(method!.ReturnType, Is.EqualTo(typeof(Task<IActionResult>)));
    }

    [Test]
    public void GetGlobal_ImaAllowAnonymous()
    {
        var method = typeof(ChatController).GetMethod(nameof(ChatController.GetGlobal));
        var allow = method?.GetCustomAttribute<AllowAnonymousAttribute>();

        Assert.That(allow, Is.Not.Null);
    }

    [Test]
    public void GetGlobal_ImaRouteGlobal()
    {
        var method = typeof(ChatController).GetMethod(nameof(ChatController.GetGlobal));
        var get = method?.GetCustomAttribute<HttpGetAttribute>();

        Assert.That(get, Is.Not.Null);
        Assert.That(get!.Template, Is.EqualTo("global"));
    }

    [Test]
    public void GetGlobal_VracaTaskIActionResult()
    {
        var method = typeof(ChatController).GetMethod(nameof(ChatController.GetGlobal));

        Assert.That(method, Is.Not.Null);
        Assert.That(method!.ReturnType, Is.EqualTo(typeof(Task<IActionResult>)));
    }

    [Test]
    public async Task SendTournament_VracaBadRequest_KadFaliTournamentId()
    {
        TestPomocnik.Ulogovan(_controller, "krle");
        var result = await _controller.SendTournament("", new ApiDtos.SendChatMessageDto("msg"));

        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task SendTournament_VracaBadRequest_KadFaliTekst()
    {
        TestPomocnik.Ulogovan(_controller, "krle");
        var result = await _controller.SendTournament("t1", new ApiDtos.SendChatMessageDto(""));

        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task SendTournament_VracaUnauthorized_KadNijeUlogovan()
    {
        TestPomocnik.NijeUlogovan(_controller);
        var result = await _controller.SendTournament("t1", new ApiDtos.SendChatMessageDto("msg"));

        Assert.That(result, Is.InstanceOf<UnauthorizedObjectResult>());
    }

    [Test]
    public void SendTournament_ImaAuthorizeIRoute()
    {
        var method = typeof(ChatController).GetMethod(nameof(ChatController.SendTournament));
        var auth = method?.GetCustomAttribute<AuthorizeAttribute>();
        var post = method?.GetCustomAttribute<HttpPostAttribute>();

        Assert.That(auth, Is.Not.Null);
        Assert.That(post, Is.Not.Null);
        Assert.That(post!.Template, Is.EqualTo("tournament/{tournamentId}"));
    }

    [Test]
    public async Task GetTournament_VracaBadRequest_KadFaliTournamentId()
    {
        var result = await _controller.GetTournament("", 5);

        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public void GetTournament_ImaAllowAnonymousIRoute()
    {
        var method = typeof(ChatController).GetMethod(nameof(ChatController.GetTournament));
        var allow = method?.GetCustomAttribute<AllowAnonymousAttribute>();
        var get = method?.GetCustomAttribute<HttpGetAttribute>();

        Assert.That(allow, Is.Not.Null);
        Assert.That(get, Is.Not.Null);
        Assert.That(get!.Template, Is.EqualTo("tournament/{tournamentId}"));
    }

    [Test]
    public void GetTournament_VracaTaskIActionResult()
    {
        var method = typeof(ChatController).GetMethod(nameof(ChatController.GetTournament));

        Assert.That(method, Is.Not.Null);
        Assert.That(method!.ReturnType, Is.EqualTo(typeof(Task<IActionResult>)));
    }
}
