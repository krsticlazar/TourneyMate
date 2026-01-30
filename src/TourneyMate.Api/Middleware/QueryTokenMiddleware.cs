public class QueryTokenMiddleware
{
    private readonly RequestDelegate _next;

    public QueryTokenMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Ako ima ?token= u URL-u
        if (context.Request.Query.TryGetValue("token", out var token))
        {
            // Dodaj Authorization header
            context.Request.Headers["Authorization"] = $"Bearer {token}";
        }

        await _next(context);
    }
}