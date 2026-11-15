namespace MorfStudios.StreamSite.Data;

/// <summary>
/// One entry in <c>data/games.json</c>: what the build can't work out by itself about a game.
/// Only games that need something have an entry. A game here that isn't finished is played,
/// so it's on Unfinished or Multiplayer once a VOD title names it.
/// </summary>
public sealed record GameInfo
{
    /// <summary>The game's name as the site shows it.</summary>
    public required string Name { get; init; }

    /// <summary>A game without an ending, like Apex Legends. On Multiplayer instead of Unfinished, unless I've finished it.</summary>
    public bool Multiplayer { get; init; }

    /// <summary>Not on Unfinished or Multiplayer, and no page. Its VODs stay in the archive.</summary>
    public bool Hidden { get; init; }

    /// <summary>Other names my VOD titles use for the game, like "Silksong" for Hollow Knight: Silksong.</summary>
    public IReadOnlyList<string> Aliases { get; init; } = [];

    /// <summary>The Twitch category's name, when it isn't the same as <see cref="Name"/>.</summary>
    public string? Twitch { get; init; }

    /// <summary>The Twitch category's ID, for when several share a name, like an old game and its remake.</summary>
    public string? TwitchId { get; init; }

    /// <summary>A picture in the repo, for games Twitch has no cover for. Path from the repo's root folder.</summary>
    public string? Cover { get; init; }
}
