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

    /// <exception cref="DataException">A file is missing or has a mistake.</exception>
    public static DataFiles Load(string repoRoot)
    {
        var finished = Read<List<FinishedGame>>(repoRoot, "data/finished-games.json", required: true) ?? [];
        var games = Read<List<GameInfo>>(repoRoot, "data/games.json", required: false) ?? [];

        var thisYear = DateTime.UtcNow.Year;
        foreach (var game in finished)
        {
            if (string.IsNullOrWhiteSpace(game.Name))
                throw new DataException("data/finished-games.json: a game has an empty name.");
            if (game.Year < 2000 || game.Year > thisYear)
                throw new DataException($"data/finished-games.json: {game.Name} has the year {game.Year}.");
        }

        var byName = new Dictionary<string, GameInfo>();
        foreach (var game in games)
        {
            if (!byName.TryAdd(Names.Key(game.Name), game))
                throw new DataException($"data/games.json: {game.Name} is in the file twice.");
        }

        return new DataFiles { FinishedGames = finished, Games = byName };
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
