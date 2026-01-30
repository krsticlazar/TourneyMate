namespace TourneyMate.Api.Configurations;

public sealed class Neo4jConfiguration
{
    public string? Uri { get; set; }
    public string? Username { get; set; }
    public string? Password { get; set; }

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Uri)) throw new InvalidOperationException("NEO4J_URI_LOCAL is required.");
        if (string.IsNullOrWhiteSpace(Username)) throw new InvalidOperationException("NEO4J_USERNAME is required.");
        if (string.IsNullOrWhiteSpace(Password)) throw new InvalidOperationException("NEO4J_PASSWORD_LOCAL is required.");
    }

    public static Neo4jConfiguration Local => new()
    {
        Uri = Environment.GetEnvironmentVariable("NEO4J_URI_LOCAL"),
        Username = Environment.GetEnvironmentVariable("NEO4J_USERNAME"),
        Password = Environment.GetEnvironmentVariable("NEO4J_PASSWORD_LOCAL")
    };
}
