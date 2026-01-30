namespace TourneyMate.Redis.Models;

public sealed record ChatMessage(
    string UserId,
    string DisplayName,
    string Text,
    DateTimeOffset TimestampUtc
);
