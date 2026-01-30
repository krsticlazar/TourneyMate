using DotNetEnv;
using TourneyMate.Api.Configurations;
using TourneyMate.Api.Services;
using TourneyMate.Redis.Config;
using TourneyMate.Redis.Infrastructure;
using TourneyMate.Redis.Repositories;
using Microsoft.AspNetCore.Authentication;
using TourneyMate.Api.Auth;
using TourneyMate.Api.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Učitaj .env iz repo root-a
try
{
    var repoRoot = Path.GetFullPath(Path.Combine(builder.Environment.ContentRootPath, "..", ".."));
    var envPath = Path.Combine(repoRoot, ".env");
    if (File.Exists(envPath)) Env.Load(envPath);
}
catch { /* ignore */ }

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// CORS za React dev server (Vite)
builder.Services.AddCors(opt =>
{
    opt.AddPolicy("dev", p => p
        .AllowAnyHeader()
        .AllowAnyMethod()
        .WithOrigins("http://localhost:5173"));
});

// Redis DI
builder.Services.AddSingleton(RedisConfig.Local);
builder.Services.AddSingleton<RedisContext>();
builder.Services.AddSingleton<LeaderboardRepository>();
builder.Services.AddSingleton<ChatRepository>();
builder.Services.AddSingleton<SessionRepository>();

// Auth
builder.Services
    .AddAuthentication("RedisSession")
    .AddScheme<AuthenticationSchemeOptions, RedisSessionAuthHandler>("RedisSession", _ => { });
builder.Services.AddAuthorization();

// Neo4j DI
builder.Services.AddSingleton(Neo4jConfiguration.Local);
builder.Services.AddSingleton<Neo4jService>();

var app = builder.Build();

app.UseCors("dev");
app.UseMiddleware<GlobalExceptionHandler>();

app.UseMiddleware<QueryTokenMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.Run();