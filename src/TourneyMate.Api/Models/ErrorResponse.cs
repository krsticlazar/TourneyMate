namespace TourneyMate.Api.Models;

public sealed record ErrorResponse(string Error, int StatusCode, string? Details = null);
