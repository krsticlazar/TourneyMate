using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TourneyMate.Api.Controllers;

namespace Api.Tests;

[TestFixture]
public class AdminControllerTests
{
    private AdminController _controller = null!;

    [SetUp]
    public void Setup()
    {
        _controller = new AdminController(null!);
    }

    [TearDown]
    public void CleanUp()
    {
        _controller = null!;
    }

    [Test]
    public void AdminKontroler_ImaAuthorizeZaAdmina()
    {
        var attr = typeof(AdminController).GetCustomAttribute<AuthorizeAttribute>();

        Assert.That(attr, Is.Not.Null);
        Assert.That(attr!.Roles, Is.EqualTo("Admin"));
    }

    [Test]
    public void AdminKontroler_ImaRouteApiAdmin()
    {
        var route = typeof(AdminController).GetCustomAttribute<RouteAttribute>();

        Assert.That(route, Is.Not.Null);
        Assert.That(route!.Template, Is.EqualTo("api/admin"));
    }

    [Test]
    public void GetAllUsers_ImaRouteUsers()
    {
        var method = typeof(AdminController).GetMethod(nameof(AdminController.GetAllUsers));
        var attr = method?.GetCustomAttribute<HttpGetAttribute>();

        Assert.That(attr, Is.Not.Null);
        Assert.That(attr!.Template, Is.EqualTo("users"));
    }

    [Test]
    public void GetAllUsers_VracaTaskIActionResult()
    {
        var method = typeof(AdminController).GetMethod(nameof(AdminController.GetAllUsers));

        Assert.That(method, Is.Not.Null);
        Assert.That(method!.ReturnType, Is.EqualTo(typeof(Task<IActionResult>)));
    }

    [Test]
    public async Task SetUserRole_VracaBadRequest_KadFaliUsername()
    {
        var result = await _controller.SetUserRole("", new AdminController.SetRoleRequest("Viewer"));

        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task SetUserRole_VracaBadRequest_KadFaliRola()
    {
        var result = await _controller.SetUserRole("mika", new AdminController.SetRoleRequest(""));

        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task SetUserRole_VracaBadRequest_KadRolaNijeValidna()
    {
        var result = await _controller.SetUserRole("mika", new AdminController.SetRoleRequest("SuperUser"));

        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public void SetUserRole_ImaRouteUsersUsernameRole()
    {
        var method = typeof(AdminController).GetMethod(nameof(AdminController.SetUserRole));
        var post = method?.GetCustomAttribute<HttpPostAttribute>();

        Assert.That(post, Is.Not.Null);
        Assert.That(post!.Template, Is.EqualTo("users/{username}/role"));
    }

    [Test]
    public void SetUserRole_VracaTaskIActionResult()
    {
        var method = typeof(AdminController).GetMethod(nameof(AdminController.SetUserRole));

        Assert.That(method, Is.Not.Null);
        Assert.That(method!.ReturnType, Is.EqualTo(typeof(Task<IActionResult>)));
    }
}
