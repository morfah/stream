using MorfStudios.StreamSite.Data;
using MorfStudios.StreamSite.Twitch;
using MorfStudios.StreamSite.Vods;

namespace MorfStudios.StreamSite.Games;

/// <summary>A game on the site, with its own page. Finishing it twice, like Elden Ring, is still one game.</summary>
public sealed class Game
{
    /// <summary>From <see cref="Names.Key"/>, so "ELDEN RING" and "Elden Ring" are the same game.</summary>
    public required string Key { get; init; }

    /// <summary>As the finished list writes it the latest time.</summary>
    public required string Name { get; init; }

    /// <summary>The page's folder, like <c>elden-ring</c>.</summary>
    public required string Slug { get; init; }

    /// <summary>Newest first, like the finished list.</summary>
    public required IReadOnlyList<FinishedGame> Finishes { get; init; }

    /// <summary>Other names my VOD titles use for it, from <c>data/games.json</c>.</summary>
    public required IReadOnlyList<string> Aliases { get; init; }

    public Cover? Cover { get; set; }

    /// <summary>Oldest first.</summary>
    public IReadOnlyList<Vod> Vods { get; set; } = [];

    /// <summary>Streams of the game whose VOD is lost, oldest first.</summary>
    public IReadOnlyList<MissingVod> MissingVods { get; set; } = [];

    /// <summary>The page's address from the site's root.</summary>
    public string Path => $"games/{Slug}/";

    /// <summary>The games on the finished list, in its order.</summary>
    public static IReadOnlyList<Game> FromData(DataFiles data)
    {
        var games = new List<Game>();
        foreach (var finishes in data.FinishedGames.GroupBy(g => Names.Key(g.Name)))
        {
            var key = finishes.Key;
            games.Add(new Game
            {
                Key = key,
                Name = finishes.First().Name,
                // Every name has Latin letters or digits so far. The fallback keeps a page if one doesn't.
                Slug = key.Length > 0 ? key.Replace(' ', '-') : $"game-{games.Count + 1}",
                Finishes = finishes.ToList(),
                Aliases = data.Games.GetValueOrDefault(key)?.Aliases ?? [],
            });
        }
        return games;
    }
}
