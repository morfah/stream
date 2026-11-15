using System.Collections.Concurrent;
using MorfStudios.StreamSite.Data;

namespace MorfStudios.StreamSite.Twitch;

/// <summary>
/// Finds each game's Twitch category and saves its box art in the site, so visitors' browsers
/// don't load anything from Twitch. A game can name its category or bring its own picture in <c>data/games.json</c>.
/// </summary>
public sealed class CoverFinder(HttpClient http, TwitchClient? twitch, string repoRoot, string siteDir, Report report)
{
    public const int Width = 285;
    public const int Height = 380;

    private string CoversDir => Path.Combine(siteDir, "covers");

    /// <summary>Covers by game, keyed with <see cref="Names.Key"/>: the path from the site's root, like <c>covers/1234.jpg</c>.</summary>
    public async Task<IReadOnlyDictionary<string, string>> FindAsync(IEnumerable<string> gameNames, IReadOnlyDictionary<string, GameInfo> info, CancellationToken ct)
    {
        Directory.CreateDirectory(CoversDir);
        var covers = new Dictionary<string, string>();
        var lookups = new List<Lookup>();

        foreach (var name in gameNames.DistinctBy(Names.Key))
        {
            var key = Names.Key(name);
            var game = info.GetValueOrDefault(key);
            if (game?.Cover is { } own)
                covers[key] = CopyOwnCover(own, key);
            else
                lookups.Add(new Lookup(key, name, game?.TwitchId, game?.Twitch ?? name));
        }

        if (twitch is null)
        {
            report.Add("Covers", [$"No Twitch keys, so {lookups.Count} games have no cover. Fill in {Keys.LocalFile} to get them."]);
            return covers;
        }

        var categories = await FindCategoriesAsync(twitch, lookups, ct);
        var saved = await DownloadBoxArtAsync(categories.Values.DistinctBy(c => c.Id), ct);

        var notFound = new List<string>();
        var noArt = new List<string>();
        foreach (var lookup in lookups)
        {
            if (!categories.TryGetValue(lookup.Key, out var category))
                notFound.Add(lookup.Name);
            else if (saved.TryGetValue(category.Id, out var path))
                covers[lookup.Key] = path;
            else
                noArt.Add($"{lookup.Name} (Twitch category \"{category.Name}\", ID {category.Id})");
        }

        report.Add("Not found on Twitch, so no cover. Add the game's Twitch name or ID in data/games.json:", notFound);
        report.Add("Twitch has no box art. Add your own picture in data/games.json:", noArt);
        report.Add("Covers", [$"{covers.Count} of {covers.Count + notFound.Count + noArt.Count} games have a cover."]);
        return covers;
    }

    /// <summary>Exact names and IDs first, then a search for the rest. Keyed by the game's key.</summary>
    private static async Task<Dictionary<string, TwitchCategory>> FindCategoriesAsync(TwitchClient twitch, List<Lookup> lookups, CancellationToken ct)
    {
        var found = await twitch.GetGamesAsync(
            lookups.Where(l => l.TwitchId is null).Select(l => l.TwitchName),
            lookups.Where(l => l.TwitchId is not null).Select(l => l.TwitchId!),
            ct);
        var byId = found.DistinctBy(c => c.Id).ToDictionary(c => c.Id);
        var byName = found.DistinctBy(c => Names.Key(c.Name)).ToDictionary(c => Names.Key(c.Name));

        var categories = new Dictionary<string, TwitchCategory>();
        var searched = new Dictionary<string, string>();
        foreach (var lookup in lookups)
        {
            var category = lookup.TwitchId is { } id ? byId.GetValueOrDefault(id) : byName.GetValueOrDefault(Names.Key(lookup.TwitchName));
            if (category is not null)
                categories[lookup.Key] = category;
            else if (lookup.TwitchId is null && await SearchAsync(twitch, lookup.TwitchName, ct) is { } searchedId)
                searched[lookup.Key] = searchedId;
        }

        // Search results only have small box art, so get those categories again.
        if (searched.Count > 0)
        {
            var again = (await twitch.GetGamesAsync([], searched.Values, ct)).DistinctBy(c => c.Id).ToDictionary(c => c.Id);
            foreach (var (key, id) in searched)
            {
                if (again.TryGetValue(id, out var category))
                    categories[key] = category;
            }
        }
        return categories;
    }

    /// <summary>
    /// Searches for a category with the same name, ignoring case, accents and punctuation, so "ELDEN RING" finds
    /// "Elden Ring". For names like "The Radio Station | 深夜放送", the part before the bar counts too.
    /// </summary>
    private static async Task<string?> SearchAsync(TwitchClient twitch, string name, CancellationToken ct)
    {
        var firstPart = name.Split(" | ")[0];
        var wanted = new HashSet<string> { Names.Key(name), Names.Key(firstPart) };
        wanted.Remove("");

        foreach (var query in new[] { name, firstPart, Names.Key(name) }.Distinct().Where(q => q.Length > 0))
        {
            var match = (await twitch.SearchAsync(query, ct)).FirstOrDefault(c => wanted.Contains(Names.Key(c.Name)));
            if (match is not null)
                return match.Id;
        }
        return null;
    }

    /// <summary>Saves each category's box art. Keyed by category ID. Twitch's "no box art" picture is left out.</summary>
    private async Task<IReadOnlyDictionary<string, string>> DownloadBoxArtAsync(IEnumerable<TwitchCategory> categories, CancellationToken ct)
    {
        var saved = new ConcurrentDictionary<string, string>();
        var options = new ParallelOptions { MaxDegreeOfParallelism = 8, CancellationToken = ct };
        await Parallel.ForEachAsync(categories, options, async (category, token) =>
        {
            var url = category.BoxArtUrl.Replace("{width}", Width.ToString()).Replace("{height}", Height.ToString());
            using var response = await http.GetAsync(url, token);
            response.EnsureSuccessStatusCode();
            // Twitch answers a category without box art with a redirect to its placeholder.
            if (response.RequestMessage?.RequestUri?.AbsolutePath.Contains("404_boxart") == true)
                return;

            var file = $"{category.Id}.jpg";
            await File.WriteAllBytesAsync(Path.Combine(CoversDir, file), await response.Content.ReadAsByteArrayAsync(token), token);
            saved[category.Id] = "covers/" + file;
        });
        return saved;
    }

    private string CopyOwnCover(string relativePath, string key)
    {
        var source = Path.Combine(repoRoot, relativePath);
        if (!File.Exists(source))
            throw new DataException($"data/games.json: the cover {relativePath} doesn't exist.");

        var file = "own-" + key.Replace(' ', '-') + Path.GetExtension(source).ToLowerInvariant();
        File.Copy(source, Path.Combine(CoversDir, file), overwrite: true);
        return "covers/" + file;
    }

    /// <param name="Key">The game's key, from <see cref="Names.Key"/>.</param>
    /// <param name="Name">The game's name as the site shows it.</param>
    /// <param name="TwitchId">The category ID from data/games.json, if any.</param>
    /// <param name="TwitchName">The category name to look up: from data/games.json, or the game's own name.</param>
    private sealed record Lookup(string Key, string Name, string? TwitchId, string TwitchName);
}
