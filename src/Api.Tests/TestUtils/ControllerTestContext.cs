using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Api.Tests.TestUtils;

internal static class TestPomocnik
{
    public static void NijeUlogovan(ControllerBase controller)
    {
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity())
            }
        };
    }

    public static void Ulogovan(
        ControllerBase controller,
        string korisnickoIme = "pera",
        string uloga = "Viewer",
        string prikaznoIme = "Pera Peric",
        string? token = null)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, korisnickoIme),
            new(ClaimTypes.Role, uloga),
            new("displayName", prikaznoIme)
        };

        if (!string.IsNullOrWhiteSpace(token))
        {
            claims.Add(new Claim("token", token));
        }

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"))
            }
        };
    }
}
