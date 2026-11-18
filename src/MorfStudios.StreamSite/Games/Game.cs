using MorfStudios.StreamSite.Data;
using MorfStudios.StreamSite.Twitch;
using MorfStudios.StreamSite.Vods;

namespace MorfStudios.StreamSite.Games;

/// <summary>The page that lists a game. Other is for streams that aren't a game, like IRL.</summary>
public enum GameList { Finished, Unfinished, Multiplayer, Other }

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

    /// <summary>Marked <c>category</c> in <c>data/games.json</c>: a kind of stream, like IRL, not a game.</summary>
    public bool Category { get; init; }

    /// <summary>Marked <c>dlc</c> in <c>data/games.json</c>: an add-on to another game, like The Frozen Wilds.</summary>
    public bool Dlc { get; init; }

    /// <summary>Marked <c>mod</c> in <c>data/games.json</c>: a game changed by fans, like Silent Hill 2: Enhanced Edition.</summary>
    public bool Mod { get; init; }

    /// <summary>My rating, 1 to 5 stars, from <c>data/games.json</c>. Null when I haven't rated it.</summary>
    public int? Rating { get; init; }

    /// <summary>
    /// Oldest first: one for each line in the finished list, and one I haven't finished, for a game I haven't finished or
    /// one I'm playing again, like Valheim co-op. A game found in the VOD titles is one playthrough I haven't finished.
    /// </summary>
    public IReadOnlyList<Playthrough> Playthroughs { get; init; } = [new() { Coop = [] }];

    /// <summary>The playthrough I haven't finished, if there is one.</summary>
    public Playthrough? Unfinished => Playthroughs.FirstOrDefault(p => p.Finish is null);

    /// <summary>Everyone I played it with, each once, from the oldest playthrough on. Empty when I played it alone.</summary>
    public IReadOnlyList<CoopPartner> Coop => Playthroughs.SelectMany(p => p.Coop).Distinct().ToList();

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
    public GameList List => IsFinished ? GameList.Finished
        : Category ? GameList.Other
        : Multiplayer ? GameList.Multiplayer
        : GameList.Unfinished;

    /// <summary>
    /// On Unfinished, Multiplayer or Other. A finished game is on Unfinished too while it has a playthrough I haven't
    /// finished with VODs, like Valheim co-op, and a finished multiplayer game, like Splitgate, is on Multiplayer too.
    /// </summary>
    public bool IsOn(GameList list) => list switch
    {
        GameList.Multiplayer => Multiplayer && LastPlayed is not null,
        GameList.Unfinished => !Category && !Multiplayer && !Hidden && Unfinished is { } unfinished
            && (VodsOf(unfinished).Count > 0 || MissingVodsOf(unfinished).Count > 0),
        _ => List == list,
    };

    /// <summary>Oldest first.</summary>
    public IReadOnlyList<Vod> VodsOf(Playthrough playthrough) => Vods.Where(v => playthrough.Has(v.Date)).ToList();

    /// <summary>Oldest first.</summary>
    public IReadOnlyList<MissingVod> MissingVodsOf(Playthrough playthrough) => MissingVods.Where(m => playthrough.Has(m.Date)).ToList();

    /// <summary>The playthrough of a line in the finished list.</summary>
    public Playthrough PlaythroughOf(FinishedGame finish) => Playthroughs.First(p => ReferenceEquals(p.Finish, finish));

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
            games.Add(Create(finishes.Key, finishes.First().Name, finishes.ToList(), data.Games.GetValueOrDefault(finishes.Key), data, games.Count));

        var finished = games.Select(g => g.Key).ToHashSet();
        foreach (var (key, info) in data.Games)
        {
            if (!finished.Contains(key))
                games.Add(Create(key, info.Name, [], info, data, games.Count));
        }
        return games;
    }

    private static Game Create(string key, string name, IReadOnlyList<FinishedGame> finishes, GameInfo? info, DataFiles data, int index) => new()
    {
        Key = key,
        Name = name,
        Slug = SlugFor(key, index),
        Finishes = finishes,
        Aliases = info?.Aliases ?? [],
        Multiplayer = info?.Multiplayer ?? false,
        Hidden = info?.Hidden ?? false,
        Category = info?.Category ?? false,
        Dlc = info?.Dlc ?? false,
        Mod = info?.Mod ?? false,
        Rating = info?.Rating,
        Playthroughs = Playthrough.Of(finishes, info, data.CoopPartners),
    };

    /// <summary>Every name has Latin letters or digits so far. The fallback keeps a page if one doesn't.</summary>
    public static string SlugFor(string key, int index) => key.Length > 0 ? key.Replace(' ', '-') : $"game-{index + 1}";
}

/// <param name="Category">The Twitch category with the same name as <paramref name="Pieces"/>.</param>
/// <param name="Pieces">The pieces of VOD titles that named it, like "Palworld" or "Final Fantasy XVI Demo".</param>
public sealed record FoundGame(TwitchCategory Category, IReadOnlyList<string> Pieces);
