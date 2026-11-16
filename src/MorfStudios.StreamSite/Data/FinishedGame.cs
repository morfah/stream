using System.Text.Json.Serialization;

namespace MorfStudios.StreamSite.Data;

/// <summary>One line in <c>data/finished-games.json</c>: a game I finished on stream.</summary>
public sealed record FinishedGame
{
    /// <summary>The day I got the credits, like 2025-10-14. The site sorts by it.</summary>
    public required DateOnly Date { get; init; }

    public required string Name { get; init; }

    /// <summary>
    /// The day the playthrough started, for a game played more than once, when its VODs don't start the day after the
    /// playthrough before it finished. Its page splits the VODs by playthrough.
    /// </summary>
    public DateOnly? Started { get; init; }

    /// <summary>The day I got every achievement or trophy, if I did. The same day as <see cref="Date"/> or later.</summary>
    public DateOnly? HundredPercent { get; init; }

    /// <summary>
    /// Who I played it with, by their names in <c>data/coop-partners.json</c>: one name, or a list for a party.
    /// After loading, written as that file does. Empty when I played alone.
    /// </summary>
    [JsonConverter(typeof(OneOrManyConverter))]
    public IReadOnlyList<string> Coop { get; init; } = [];

    /// <summary>Shown as "<c>Difficulty</c> difficulty", like Hardest or Nightmare.</summary>
    public string? Difficulty { get; init; }

    /// <summary>The year on the Finished page. Not in the file, so an old line with "year" stops the build.</summary>
    [JsonIgnore]
    public int Year => Date.Year;
}
