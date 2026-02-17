using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using TourneyMate.Api.Controllers;

namespace Api.Tests;

[TestFixture]
public class HomeControllerTests
{
    [Test]
    public void HomeKontroler_ImaRouteApiHome()
    {
        var route = typeof(HomeController).GetCustomAttribute<RouteAttribute>();

        Assert.That(route, Is.Not.Null);
        Assert.That(route!.Template, Is.EqualTo("api/home"));
    }

    [Test]
    public void GetHome_ImaHttpGetAtribut()
    {
        var method = typeof(HomeController).GetMethod(nameof(HomeController.GetHome));
        var get = method?.GetCustomAttribute<HttpGetAttribute>();

        Assert.That(get, Is.Not.Null);
    }

    [Test]
    public void GetHome_VracaTaskIActionResult()
    {
        var method = typeof(HomeController).GetMethod(nameof(HomeController.GetHome));

        Assert.That(method, Is.Not.Null);
        Assert.That(method!.ReturnType, Is.EqualTo(typeof(Task<IActionResult>)));
    }

    [Test]
    public void HomeKontroler_ImaApiControllerAtribut()
    {
        var attr = typeof(HomeController).GetCustomAttribute<ApiControllerAttribute>();

        Assert.That(attr, Is.Not.Null);
    }

    [Test]
    public void GetHome_ParametriImajuPodrazumevaneVrednosti()
    {
        var method = typeof(HomeController).GetMethod(nameof(HomeController.GetHome));
        var parameters = method!.GetParameters();

        Assert.That(parameters[0].Name, Is.EqualTo("topN"));
        Assert.That(parameters[0].DefaultValue, Is.EqualTo(5));
        Assert.That(parameters[1].Name, Is.EqualTo("chatN"));
        Assert.That(parameters[1].DefaultValue, Is.EqualTo(30));
    }

    [Test]
    public void GetHome_ParametriImajuFromQuery()
    {
        var method = typeof(HomeController).GetMethod(nameof(HomeController.GetHome));
        var parameters = method!.GetParameters();

        var topNAttr = parameters[0].GetCustomAttribute<FromQueryAttribute>();
        var chatNAttr = parameters[1].GetCustomAttribute<FromQueryAttribute>();

        Assert.That(topNAttr, Is.Not.Null);
        Assert.That(chatNAttr, Is.Not.Null);
    }
}
