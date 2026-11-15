namespace MorfStudios.StreamSite.Data;

/// <summary>
/// One entry in <c>data/missing-vods.json</c>: a stream whose VOD is lost, like one not exported from
/// Twitch in time. Its games' pages show it in the VOD list, so the gap is explained.
/// </summary>
public sealed record MissingVod
{
    public required DateOnly Date { get; init; }

    /// <summary>Names as in the finished list, or other names from <c>data/games.json</c>.</summary>
    public required IReadOnlyList<string> Games { get; init; }

    /// <summary>Like "Silksong - Part 23". When left out, the page shows the games' names.</summary>
    public string? Title { get; init; }
}
