using System.Reflection;
using Api.Tests.TestUtils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TourneyMate.Api.Controllers;

namespace Api.Tests;

[TestFixture]
public class AuthControllerTests
{
    private AuthController _controller = null!;

    [SetUp]
    public void Setup()
    {
        _controller = new AuthController(null!, null!);
    }

    [TearDown]
    public void CleanUp()
    {
        _controller = null!;
    }

    [Test]
    public async Task Registracija_VracaBadRequest_KadFaliUsername()
    {
        var result = await _controller.Register(new AuthController.RegisterRequest("", "tajna12", "Pera"));
        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task Registracija_VracaBadRequest_KadJeSifraKratka()
    {
        var result = await _controller.Register(new AuthController.RegisterRequest("pera", "123", "Pera"));
        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task Registracija_VracaBadRequest_KadFaliDisplayName()
    {
        var result = await _controller.Register(new AuthController.RegisterRequest("pera", "tajna12", ""));
        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public void Registracija_ImaRouteRegister()
    {
        var method = typeof(AuthController).GetMethod(nameof(AuthController.Register));
        var post = method?.GetCustomAttribute<HttpPostAttribute>();
        Assert.That(post, Is.Not.Null);
        Assert.That(post!.Template, Is.EqualTo("register"));
    }

    [Test]
    public void AuthKontroler_ImaRouteApiAuth()
    {
        var route = typeof(AuthController).GetCustomAttribute<RouteAttribute>();
        Assert.That(route, Is.Not.Null);
        Assert.That(route!.Template, Is.EqualTo("api/auth"));
    }

    [Test]
    public void Registracija_VracaTaskIActionResult()
    {
        var method = typeof(AuthController).GetMethod(nameof(AuthController.Register));
        Assert.That(method, Is.Not.Null);
        Assert.That(method!.ReturnType, Is.EqualTo(typeof(Task<IActionResult>)));
    }

    [Test]
    public async Task Login_VracaBadRequest_KadFaliUsername()
    {
        var result = await _controller.Login(new AuthController.LoginRequest("", "tajna12"));
        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task Login_VracaBadRequest_KadFaliSifra()
    {
        var result = await _controller.Login(new AuthController.LoginRequest("pera", ""));
        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public void Login_ImaRouteLogin()
    {
        var method = typeof(AuthController).GetMethod(nameof(AuthController.Login));
        var post = method?.GetCustomAttribute<HttpPostAttribute>();
        Assert.That(post, Is.Not.Null);
        Assert.That(post!.Template, Is.EqualTo("login"));
    }

    [Test]
    public void Login_VracaTaskIActionResult()
    {
        var method = typeof(AuthController).GetMethod(nameof(AuthController.Login));
        Assert.That(method, Is.Not.Null);
        Assert.That(method!.ReturnType, Is.EqualTo(typeof(Task<IActionResult>)));
    }

    [Test]
    public void Me_VracaOkIZaglavljeSaUserPodacima()
    {
        TestPomocnik.Ulogovan(_controller, "krle", "Viewer", "Krle");
        var result = _controller.Me() as OkObjectResult;
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Value!.ToString(), Does.Contain("krle"));
    }

    [Test]
    public void Me_VracaOk_IKadNemaUsera()
    {
        TestPomocnik.NijeUlogovan(_controller);
        var result = _controller.Me();
        Assert.That(result, Is.InstanceOf<OkObjectResult>());
    }

    [Test]
    public void Me_ImaAuthorizeIRoute()
    {
        var method = typeof(AuthController).GetMethod(nameof(AuthController.Me));
        var authorize = method?.GetCustomAttribute<AuthorizeAttribute>();
        var get = method?.GetCustomAttribute<HttpGetAttribute>();
        Assert.That(authorize, Is.Not.Null);
        Assert.That(get, Is.Not.Null);
        Assert.That(get!.Template, Is.EqualTo("me"));
    }

    [Test]
    public async Task Logout_VracaOk_KadNemaToken()
    {
        TestPomocnik.Ulogovan(_controller, "krle", token: null);
        var result = await _controller.Logout();
        Assert.That(result, Is.InstanceOf<OkObjectResult>());
    }

    [Test]
    public void Logout_ImaAuthorizeIRoute()
    {
        var method = typeof(AuthController).GetMethod(nameof(AuthController.Logout));
        var authorize = method?.GetCustomAttribute<AuthorizeAttribute>();
        var post = method?.GetCustomAttribute<HttpPostAttribute>();
        Assert.That(authorize, Is.Not.Null);
        Assert.That(post, Is.Not.Null);
        Assert.That(post!.Template, Is.EqualTo("logout"));
    }

    [Test]
    public void Logout_VracaTaskIActionResult()
    {
        var method = typeof(AuthController).GetMethod(nameof(AuthController.Logout));
        Assert.That(method, Is.Not.Null);
        Assert.That(method!.ReturnType, Is.EqualTo(typeof(Task<IActionResult>)));
    }
}
