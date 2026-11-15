using System.Collections.Concurrent;
using MorfStudios.StreamSite.Data;
using MorfStudios.StreamSite.Twitch;
using MorfStudios.StreamSite.Vods;

namespace MorfStudios.StreamSite.Games;

/// <summary>
/// Finds the games I've played that no data file names: a piece of a VOD title with the same name as a Twitch category,
/// like "Palworld" in <c>Palworld - 2024-02-13</c>. Nothing is stored, so every build finds them again, and a new game
/// shows up by itself. Pieces that aren't a game I played go in <c>data/not-games.json</c>.
/// </summary>
public static class GameFinder
{
    /// <summary>The games found, sorted by name. None without the Twitch keys.</summary>
    /// <param name="games">The games the data files know. Their names and other names aren't looked up.</param>
    public static async Task<IReadOnlyList<Game>> FindAsync(TwitchClient? twitch, IReadOnlyList<Vod> vods, IReadOnlyList<Game> games, DataFiles data, Report report, CancellationToken ct)
    {
        if (twitch is null)
        {
            report.Add("Found games", ["No Twitch keys, so only games in the data files are on the site."]);
            return [];
        }

        var knownNames = games.SelectMany(g => g.Aliases.Prepend(g.Name)).ToList();
        var taken = knownNames.Select(Names.Key).ToHashSet();
        var splitter = new TitlePieces(knownNames);

        // A fixed VOD's games come from its fix, so its title doesn't count. Names in the fixes and missing VODs do,
        // like DEVOUR, which no title names.
        var pieces = vods.Where(v => !data.VodFixes.ContainsKey(v.Id)).SelectMany(v => splitter.Find(v.ShortTitle))
            .Concat(data.VodFixes.Values.SelectMany(names => names).Concat(data.MissingVods.SelectMany(m => m.Games)).Select(name => new TitlePiece(name, [])))
            .ToList();

        bool Wanted(string? text) => text is not null && !taken.Contains(Names.Key(text)) && !data.NotGames.Contains(Names.Key(text));
        var lookups = pieces.SelectMany(p => p.Parts.Prepend(p.Whole)).Where(Wanted).OfType<string>().DistinctBy(Names.Key).ToList();
        var categories = await FindCategoriesAsync(twitch, lookups, ct);

        // A whole piece that's a game wins over its parts, so "Papers, Please" isn't "Papers" and "Please".
        var found = new Dictionary<string, (TwitchCategory Category, List<string> Pieces)>();
        foreach (var piece in pieces)
        {
            if (Wanted(piece.Whole) && categories.TryGetValue(Names.Key(piece.Whole!), out var category))
                Add(category, piece.Whole!);
            else if (piece.Whole is null || Wanted(piece.Whole))
            {
                foreach (var part in piece.Parts.Where(Wanted))
                {
                    if (categories.TryGetValue(Names.Key(part), out var partCategory))
                        Add(partCategory, part);
                }
            }
        }

        void Add(TwitchCategory category, string piece)
        {
            if (!found.TryGetValue(category.Id, out var entry))
                found[category.Id] = entry = (category, []);
            if (!entry.Pieces.Contains(piece, StringComparer.OrdinalIgnoreCase))
                entry.Pieces.Add(piece);
        }

        var newGames = new List<Game>();
        foreach (var (category, foundIn) in found.Values.OrderBy(f => f.Category.Name, StringComparer.OrdinalIgnoreCase))
        {
            var key = Names.Key(category.Name);
            // Twitch's name for a game the data files know by another name. Rare: the piece would be that game's name.
            if (!taken.Add(key))
                continue;
            // The pieces are other names, for "The Bathhouse" when Twitch calls it "The Bathhouse | 地獄銭湯♨️".
            var aliases = foundIn.Where(p => taken.Add(Names.Key(p))).ToList();
            newGames.Add(new Game
            {
                Key = key,
                Name = category.Name,
                Slug = Game.SlugFor(key, games.Count + newGames.Count),
                Finishes = [],
                Aliases = aliases,
                Found = new FoundGame(category, foundIn),
            });
        }
        return newGames;
    }

    /// <summary>
    /// Categories by the piece's <see cref="Names.Key"/>: exact names first, a hundred per call, then a search for each of the
    /// rest, which only counts a category with the same name (<see cref="TwitchCategory.SameName"/>).
    /// </summary>
    private static async Task<IReadOnlyDictionary<string, TwitchCategory>> FindCategoriesAsync(TwitchClient twitch, IReadOnlyList<string> names, CancellationToken ct)
    {
        var exact = await twitch.GetGamesAsync(names, [], ct);
        var categories = new ConcurrentDictionary<string, TwitchCategory>();
        var rest = new List<string>();
        foreach (var name in names)
        {
            if (TwitchCategory.SameName(exact, name) is { } category)
                categories[Names.Key(name)] = category;
            else
                rest.Add(name);
        }

        var options = new ParallelOptions { MaxDegreeOfParallelism = 8, CancellationToken = ct };
        await Parallel.ForEachAsync(rest, options, async (name, token) =>
        {
            if (TwitchCategory.SameName(await twitch.SearchAsync(name, token), name) is { } category)
                categories[Names.Key(name)] = category;
        });
        return categories;
    }
}
