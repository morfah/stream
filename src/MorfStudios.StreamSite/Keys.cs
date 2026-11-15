using System.Text.Json;
using MorfStudios.StreamSite.Data;

namespace MorfStudios.StreamSite;

/// <summary>
/// API keys. GitHub Actions passes them as environment variables from the repo secrets.
/// On my PC they come from <c>keys.local.json</c>, which git ignores.
/// </summary>
public sealed record Keys(string? TwitchClientId, string? TwitchClientSecret)
{
    public const string LocalFile = "keys.local.json";

    public bool HasTwitch => TwitchClientId is not null && TwitchClientSecret is not null;

    public static Keys Load(string repoRoot)
    {
        var local = ReadLocalFile(Path.Combine(repoRoot, LocalFile));
        string? Get(string name)
        {
            var value = Environment.GetEnvironmentVariable(name);
            if (string.IsNullOrWhiteSpace(value))
                local.TryGetValue(name, out value);
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        return new Keys(Get("TWITCH_CLIENT_ID"), Get("TWITCH_CLIENT_SECRET"));
    }

    private static Dictionary<string, string> ReadLocalFile(string path)
    {
        if (!File.Exists(path))
            return [];
        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(path)) ?? [];
        }
        catch (JsonException ex)
        {
            throw new DataException($"{LocalFile}: {ex.Message}");
        }
    }
}
