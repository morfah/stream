using System.Text.Json.Serialization;

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

    /// <summary>
    /// Not a game but a kind of stream, like Software and Game Development or IRL. Listed on Other
    /// instead of Unfinished.
    /// </summary>
    public bool Category { get; init; }

    public const int MinRating = 1;
    public const int MaxRating = 5;

    /// <summary>
    /// My rating, 1 to 5 stars, like on Grouvee, where I've rated the games I play since before I started streaming.
    /// The stars are on the game's cover and its page.
    /// </summary>
    public int? Rating { get; init; }

    /// <summary>
    /// Who I played the playthrough I haven't finished with, like <c>coop</c> in the finished list. For a finished game,
    /// that's one after the finish, like Valheim co-op after my solo one, so it needs <see cref="Started"/>. The finished
    /// ones' partners go on their lines in the finished list.
    /// </summary>
    [JsonConverter(typeof(OneOrManyConverter))]
    public IReadOnlyList<string> Coop { get; init; } = [];

    /// <summary>
    /// The day the playthrough I haven't finished started. For a finished game, it adds that playthrough, which has the
    /// VODs from that day on.
    /// </summary>
    public DateOnly? Started { get; init; }

    /// <summary>Other names my VOD titles use for the game, like "Silksong" for Hollow Knight: Silksong.</summary>
    public IReadOnlyList<string> Aliases { get; init; } = [];

    /// <summary>The Twitch category's name, when it isn't the same as <see cref="Name"/>.</summary>
    public string? Twitch { get; init; }

    /// <summary>The Twitch category's ID, for when several share a name, like an old game and its remake.</summary>
    public string? TwitchId { get; init; }

    /// <summary>A picture in the repo, for games Twitch has no cover for. Path from the repo's root folder.</summary>
    public string? Cover { get; init; }
}
