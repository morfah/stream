using System.Text.Json;
using System.Text.Json.Serialization;

namespace MorfStudios.StreamSite.Data;

/// <summary>The JSON files in the <c>data</c> folder, which I edit by hand.</summary>
public sealed class DataFiles
{
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

    /// <exception cref="DataException">A file is missing or has a mistake.</exception>
    public static DataFiles Load(string repoRoot)
    {
        var finished = Read<List<FinishedGame>>(repoRoot, "data/finished-games.json", required: true) ?? [];
        var games = Read<List<GameInfo>>(repoRoot, "data/games.json", required: false) ?? [];
        var fixes = Read<Dictionary<string, IReadOnlyList<string>>>(repoRoot, "data/vod-fixes.json", required: false) ?? [];
        var missing = Read<List<MissingVod>>(repoRoot, "data/missing-vods.json", required: false) ?? [];
        var notGames = Read<List<string>>(repoRoot, "data/not-games.json", required: false) ?? [];

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
        }
        // Newest first, whatever the file's order. Finishes on the same day keep the file's order.
        finished = finished.OrderByDescending(g => g.Date).ToList();

        var byName = new Dictionary<string, GameInfo>();
        foreach (var game in games)
        {
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
}

/// <summary>A data file is missing or has a mistake. The message says where.</summary>
public sealed class DataException(string message) : Exception(message);
