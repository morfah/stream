using System.Text.Json.Serialization;

namespace MorfStudios.StreamSite.Data;

/// <summary>One line in <c>data/finished-games.json</c>: a game I finished on stream.</summary>
public sealed record FinishedGame
{
    /// <summary>The day I got the credits, like 2025-10-14. The site sorts by it.</summary>
    public required DateOnly Date { get; init; }

    public required string Name { get; init; }

    /// <summary>The day I got every achievement or trophy, if I did. The same day as <see cref="Date"/> or later.</summary>
    public DateOnly? HundredPercent { get; init; }

    /// <summary>The Twitch name of who I played it with.</summary>
    public string? Coop { get; init; }

    /// <summary>Shown as "<c>Difficulty</c> difficulty", like Hardest or Nightmare.</summary>
    public string? Difficulty { get; init; }

    /// <summary>The year on the Finished page. Not in the file, so an old line with "year" stops the build.</summary>
    [JsonIgnore]
    public int Year => Date.Year;
}
