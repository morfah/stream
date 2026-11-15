namespace MorfStudios.StreamSite.Data;

/// <summary>One line in <c>data/finished-games.json</c>: a game I finished on stream.</summary>
public sealed record FinishedGame
{
    public required int Year { get; init; }
    public required string Name { get; init; }

    /// <summary>Finished with every achievement or trophy.</summary>
    public bool HundredPercent { get; init; }

    /// <summary>The Twitch name of who I played it with.</summary>
    public string? Coop { get; init; }

    /// <summary>Shown as "<c>Difficulty</c> difficulty", like Hardest or Nightmare.</summary>
    public string? Difficulty { get; init; }
}
