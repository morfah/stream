using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace MorfStudios.StreamSite.Data;

/// <summary>The JSON files in the <c>data</c> folder, which I edit by hand.</summary>
public sealed partial class DataFiles
{
    private const string PartnersFile = "data/coop-partners.json";

    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        RespectNullableAnnotations = true,
        // A misspelled field stops the build instead of being ignored.
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
    };

    public required IReadOnlyList<FinishedGame> FinishedGames { get; init; }

    /// <summary>Keyed by the game's name, compared with <see cref="Names.Key"/>.</summary>
    public required IReadOnlyDictionary<string, GameInfo> Games { get; init; }

    /// <summary>
    /// From <c>data/vod-fixes.json</c>: a VOD's YouTube ID and the names of the games it really has,
    /// replacing what its title gave. An empty list means no game.
    /// </summary>
    public required IReadOnlyDictionary<string, IReadOnlyList<string>> VodFixes { get; init; }

    /// <summary>From <c>data/missing-vods.json</c>: streams whose VOD is lost.</summary>
    public required IReadOnlyList<MissingVod> MissingVods { get; init; }

    /// <summary>
    /// From <c>data/not-games.json</c>: pieces of VOD titles that have a Twitch category's name but aren't a game
    /// I played, so the build doesn't make a game of them. Compared with <see cref="Names.Key"/>.
    /// </summary>
    public required IReadOnlySet<string> NotGames { get; init; }

    /// <summary>From <c>data/coop-partners.json</c>, keyed by their names, compared with <see cref="Names.Key"/>.</summary>
    public required IReadOnlyDictionary<string, CoopPartner> CoopPartners { get; init; }

    /// <exception cref="DataException">A file is missing or has a mistake.</exception>
    public static DataFiles Load(string repoRoot)
    {
        var finished = Read<List<FinishedGame>>(repoRoot, "data/finished-games.json", required: true) ?? [];
        var games = Read<List<GameInfo>>(repoRoot, "data/games.json", required: false) ?? [];
        var fixes = Read<Dictionary<string, IReadOnlyList<string>>>(repoRoot, "data/vod-fixes.json", required: false) ?? [];
        var missing = Read<List<MissingVod>>(repoRoot, "data/missing-vods.json", required: false) ?? [];
        var notGames = Read<List<string>>(repoRoot, "data/not-games.json", required: false) ?? [];
        var partners = Read<List<CoopPartner>>(repoRoot, PartnersFile, required: false) ?? [];

        var partnersByName = new Dictionary<string, CoopPartner>();
        foreach (var partner in partners)
        {
            if (Names.Key(partner.Name).Length == 0)
                throw new DataException($"{PartnersFile}: someone has an empty name.");
            if (!partnersByName.TryAdd(Names.Key(partner.Name), partner))
                throw new DataException($"{PartnersFile}: {partner.Name} is in the file twice.");
            // Twitch names are letters, digits and underscores, so a link or a display name with spaces is a mistake.
            if (partner.Twitch is { } twitch && !TwitchName().IsMatch(twitch))
                throw new DataException($"{PartnersFile}: {partner.Name} has the Twitch name \"{twitch}\". It should be only the name, like \"Nyakuzaa\", not a link.");
        }

        // Each name in "coop" must be someone in data/coop-partners.json, and is written as that file does.
        IReadOnlyList<string> Party(string file, string game, IReadOnlyList<string> names) => names.Select(name =>
            partnersByName.TryGetValue(Names.Key(name ?? ""), out var partner) ? partner.Name
                : throw new DataException($"{file}: {game} has \"{name}\" in coop, who isn't in {PartnersFile}. Check the spelling, or add them there.")).Distinct().ToList();
        finished = finished.Select(g => g.Coop.Count > 0 ? g with { Coop = Party("data/finished-games.json", g.Name, g.Coop) } : g).ToList();
        games = games.Select(g => g.Coop.Count > 0 ? g with { Coop = Party("data/games.json", g.Name, g.Coop) } : g).ToList();

        // Tomorrow in UTC is today in Sweden until 01:00 or 02:00.
        var tomorrow = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1);
        foreach (var game in finished)
        {
            if (string.IsNullOrWhiteSpace(game.Name))
                throw new DataException("data/finished-games.json: a game has an empty name.");
            if (game.Date.Year < Vods.Vod.FirstYear || game.Date > tomorrow)
                throw new DataException($"data/finished-games.json: {game.Name} has the date {game.Date:yyyy-MM-dd}, which can't be right.");
            if (game.HundredPercent is { } hundred && (hundred < game.Date || hundred > tomorrow))
                throw new DataException($"data/finished-games.json: {game.Name} has 100% on {hundred:yyyy-MM-dd}, which can't be right with the credits on {game.Date:yyyy-MM-dd}.");
            if (game.Started is { } started && (started.Year < Vods.Vod.FirstYear || started > game.Date))
                throw new DataException($"data/finished-games.json: {game.Name} has started on {started:yyyy-MM-dd}, which can't be right with the credits on {game.Date:yyyy-MM-dd}.");
        }
        // Newest first, whatever the file's order. Finishes on the same day keep the file's order.
        finished = finished.OrderByDescending(g => g.Date).ToList();

        var finishedNames = finished.Select(g => Names.Key(g.Name)).ToHashSet();
        var byName = new Dictionary<string, GameInfo>();
        foreach (var game in games)
        {
            // Like DEVOUR's co-op left here after I finish it, which would make an empty playthrough after the credits.
            if (game.Coop.Count > 0 && game.Started is null && finishedNames.Contains(Names.Key(game.Name)))
                throw new DataException($"data/games.json: {game.Name} is finished, so coop here is for a playthrough I haven't finished, and needs \"started\" with the day it started. If I finished it with them, coop goes on its line in data/finished-games.json instead.");
            if (game.Started is { } started && (started.Year < Vods.Vod.FirstYear || started > tomorrow))
                throw new DataException($"data/games.json: {game.Name} has started on {started:yyyy-MM-dd}, which can't be right.");
            if (game.Rating is { } rating and not (>= GameInfo.MinRating and <= GameInfo.MaxRating))
                throw new DataException($"data/games.json: {game.Name} has the rating {rating}. It should be {GameInfo.MinRating} to {GameInfo.MaxRating} stars.");
            if (game.Dlc && game.Mod)
                throw new DataException($"data/games.json: {game.Name} is marked both dlc and mod. It can only be one of them.");
            if (!byName.TryAdd(Names.Key(game.Name), game))
                throw new DataException($"data/games.json: {game.Name} is in the file twice.");
        }

        return new DataFiles
        {
            FinishedGames = finished,
            Games = byName,
            VodFixes = fixes,
            MissingVods = missing,
            NotGames = notGames.Select(Names.Key).ToHashSet(),
            CoopPartners = partnersByName,
        };
    }

    private static T? Read<T>(string repoRoot, string relativePath, bool required) where T : class
    {
        var path = Path.Combine(repoRoot, relativePath);
        if (!File.Exists(path))
            return required ? throw new DataException($"{relativePath} is missing.") : null;

        try
        {
            return JsonSerializer.Deserialize<T>(File.ReadAllText(path), Options);
        }
        catch (JsonException ex)
        {
            var line = ex.LineNumber is { } number ? $", line {number + 1}" : "";
            throw new DataException($"{relativePath}{line}: {ex.Message}");
        }
    }

    [GeneratedRegex("^[A-Za-z0-9_]+$")]
    private static partial Regex TwitchName();
}

/// <summary>A data file is missing or has a mistake. The message says where.</summary>
public sealed class DataException(string message) : Exception(message);
