using MorfStudios.StreamSite.Data;
using MorfStudios.StreamSite.Twitch;
using MorfStudios.StreamSite.Vods;

namespace MorfStudios.StreamSite.Games;

/// <summary>The page that lists a game.</summary>
public enum GameList { Finished, Unfinished, Multiplayer }

/// <summary>What happened in a VOD, from the dates in the finished list.</summary>
[Flags]
public enum Milestone { None = 0, Finished = 1, HundredPercent = 2 }

/// <summary>
/// A game I've played on stream, with its own page. Finishing it twice, like Elden Ring, is still one game.
/// From the finished list, <c>data/games.json</c>, or a VOD title naming a Twitch category (<see cref="GameFinder"/>).
/// </summary>
public sealed class Game
{
    /// <summary>From <see cref="Names.Key"/>, so "ELDEN RING" and "Elden Ring" are the same game.</summary>
    public required string Key { get; init; }

    /// <summary>As the finished list writes it the latest time, otherwise as <c>data/games.json</c> or Twitch does.</summary>
    public required string Name { get; init; }

    /// <summary>The page's folder, like <c>elden-ring</c>.</summary>
    public required string Slug { get; init; }

    /// <summary>Newest first, like the finished list. Empty for a game I haven't finished.</summary>
    public required IReadOnlyList<FinishedGame> Finishes { get; init; }

    /// <summary>Other names my VOD titles use for it, from <c>data/games.json</c>.</summary>
    public required IReadOnlyList<string> Aliases { get; init; }

    /// <summary>Marked <c>multiplayer</c> in <c>data/games.json</c>.</summary>
    public bool Multiplayer { get; init; }

    /// <summary>Marked <c>hidden</c> in <c>data/games.json</c>. Only counts for a game I haven't finished.</summary>
    public bool Hidden { get; init; }

    /// <summary>How the build found the game, when no data file names it.</summary>
    public FoundGame? Found { get; init; }

    public Cover? Cover { get; set; }

    /// <summary>Oldest first.</summary>
    public IReadOnlyList<Vod> Vods { get; set; } = [];

    /// <summary>Streams of the game whose VOD is lost, oldest first.</summary>
    public IReadOnlyList<MissingVod> MissingVods { get; set; } = [];

    public bool IsFinished => Finishes.Count > 0;

    /// <summary>
    /// The list the game's page belongs to. A finished multiplayer game, like Splitgate, is on Multiplayer too,
    /// but its page belongs to Finished.
    /// </summary>
    public GameList List => IsFinished ? GameList.Finished : Multiplayer ? GameList.Multiplayer : GameList.Unfinished;

    /// <summary>On Unfinished or Multiplayer.</summary>
    public bool IsOn(GameList list) => list == GameList.Multiplayer ? Multiplayer && LastPlayed is not null : List == list;

    /// <summary>What happened in the VOD: the credits or 100%. Only the game's last VOD on such a day counts.</summary>
    public Milestone MilestonesOf(Vod vod) => Vods.LastOrDefault(v => v.Date == vod.Date) == vod ? MilestonesOn(vod.Date) : Milestone.None;

    /// <summary>The same for a lost VOD, on a day without a VOD of the game, like The Ghost Train's.</summary>
    public Milestone MilestonesOf(MissingVod missing) =>
        !Vods.Any(v => v.Date == missing.Date) && MissingVods.LastOrDefault(m => m.Date == missing.Date) == missing ? MilestonesOn(missing.Date) : Milestone.None;

    private Milestone MilestonesOn(DateOnly day) =>
        (Finishes.Any(f => f.Date == day) ? Milestone.Finished : Milestone.None) |
        (Finishes.Any(f => f.HundredPercent == day) ? Milestone.HundredPercent : Milestone.None);

    /// <summary>
    /// Finished games always have a page. Others need a VOD, or a missing one, and mustn't be hidden.
    /// Games without a page still claim their VODs, so a hidden game's VODs don't land on another game.
    /// </summary>
    public bool HasPage => IsFinished || (!Hidden && (Vods.Count > 0 || MissingVods.Count > 0));

    /// <summary>The day of the newest VOD, or missing VOD.</summary>
    public DateOnly? LastPlayed => Vods.Select(v => (DateOnly?)v.Date).Concat(MissingVods.Select(m => (DateOnly?)m.Date)).Max();

    /// <summary>The page's address from the site's root.</summary>
    public string Path => $"games/{Slug}/";

    /// <summary>The games on the finished list, in its order, then the other games in <c>data/games.json</c>.</summary>
    public static List<Game> FromData(DataFiles data)
    {
        var games = new List<Game>();
        foreach (var finishes in data.FinishedGames.GroupBy(g => Names.Key(g.Name)))
            games.Add(Create(finishes.Key, finishes.First().Name, finishes.ToList(), data.Games.GetValueOrDefault(finishes.Key), games.Count));

        var finished = games.Select(g => g.Key).ToHashSet();
        foreach (var (key, info) in data.Games)
        {
            if (!finished.Contains(key))
                games.Add(Create(key, info.Name, [], info, games.Count));
        }
        return games;
    }

    private static Game Create(string key, string name, IReadOnlyList<FinishedGame> finishes, GameInfo? info, int index) => new()
    {
        Key = key,
        Name = name,
        Slug = SlugFor(key, index),
        Finishes = finishes,
        Aliases = info?.Aliases ?? [],
        Multiplayer = info?.Multiplayer ?? false,
        Hidden = info?.Hidden ?? false,
    };

    /// <summary>Every name has Latin letters or digits so far. The fallback keeps a page if one doesn't.</summary>
    public static string SlugFor(string key, int index) => key.Length > 0 ? key.Replace(' ', '-') : $"game-{index + 1}";
}

/// <param name="Category">The Twitch category with the same name as <paramref name="Pieces"/>.</param>
/// <param name="Pieces">The pieces of VOD titles that named it, like "Palworld" or "Final Fantasy XVI Demo".</param>
public sealed record FoundGame(TwitchCategory Category, IReadOnlyList<string> Pieces);
