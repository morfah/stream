namespace MorfStudios.StreamSite.Data;

/// <summary>
/// One entry in <c>data/games.json</c>: what the build can't work out by itself about a game.
/// Only games that need something have an entry.
/// </summary>
public sealed record GameInfo
{
    /// <summary>The game's name as the site shows it.</summary>
    public required string Name { get; init; }

    /// <summary>The Twitch category's name, when it isn't the same as <see cref="Name"/>.</summary>
    public string? Twitch { get; init; }

    /// <summary>The Twitch category's ID, for when several share a name, like an old game and its remake.</summary>
    public string? TwitchId { get; init; }

    /// <summary>A picture in the repo, for games Twitch has no cover for. Path from the repo's root folder.</summary>
    public string? Cover { get; init; }
}
