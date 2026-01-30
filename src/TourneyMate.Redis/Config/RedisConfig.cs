namespace TourneyMate.Redis.Config;

public sealed class RedisConfig
{
    public string Host { get; init; } = "localhost";
    public int Port { get; init; } = 6379;
    public int Database { get; init; } = 0;

    public string ConnectionString => $"{Host}:{Port},abortConnect=false";

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Host))
            throw new InvalidOperationException("REDIS_HOST is required.");
        if (Port <= 0)
            throw new InvalidOperationException("REDIS_PORT must be > 0.");
    }

    public static RedisConfig Local => new()
    {
        Host = Environment.GetEnvironmentVariable("REDIS_HOST") ?? "localhost",
        Port = int.TryParse(Environment.GetEnvironmentVariable("REDIS_PORT"), out var p) ? p : 6379,
        Database = int.TryParse(Environment.GetEnvironmentVariable("REDIS_DB"), out var db) ? db : 0,
    };
}
