using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MorfStudios.StreamSite.Twitch;

/// <summary>
/// Twitch's API with an app token (client credentials), which needs the site's confidential Twitch app.
/// Only reads public data: game categories and their box art.
/// </summary>
public sealed class TwitchClient(HttpClient http, string clientId, string clientSecret)
{
    private const int MaxPerRequest = 100;

    private static readonly JsonSerializerOptions Json = new() { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower };

    private string? _token;

    /// <summary>Categories by exact name or ID. Names and IDs Twitch doesn't know are left out.</summary>
    public async Task<IReadOnlyList<TwitchCategory>> GetGamesAsync(IEnumerable<string> names, IEnumerable<string> ids, CancellationToken ct)
    {
        var parameters = names.Distinct().Select(n => "name=" + Uri.EscapeDataString(n))
            .Concat(ids.Distinct().Select(id => "id=" + Uri.EscapeDataString(id)))
            .ToList();

        var games = new List<TwitchCategory>();
        foreach (var chunk in parameters.Chunk(MaxPerRequest))
        {
            var response = await GetAsync<HelixResponse<TwitchCategory>>("games?" + string.Join('&', chunk), ct);
            games.AddRange(response.Data);
        }
        return games;
    }

    /// <summary>
    /// Categories whose name is like <paramref name="query"/>. Their box art links have a fixed small size,
    /// so get the categories again with <see cref="GetGamesAsync"/> for covers.
    /// </summary>
    public async Task<IReadOnlyList<TwitchCategory>> SearchAsync(string query, CancellationToken ct)
    {
        var response = await GetAsync<HelixResponse<TwitchCategory>>($"search/categories?first=50&query={Uri.EscapeDataString(query)}", ct);
        return response.Data;
    }

    private async Task<T> GetAsync<T>(string pathAndQuery, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://api.twitch.tv/helix/" + pathAndQuery);
        request.Headers.Add("Client-Id", clientId);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", await GetTokenAsync(ct));

        using var response = await http.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
            throw new TwitchException($"Twitch answered {(int)response.StatusCode} to {pathAndQuery.Split('?')[0]}: {await response.Content.ReadAsStringAsync(ct)}");
        return await response.Content.ReadFromJsonAsync<T>(Json, ct)
            ?? throw new TwitchException($"Twitch sent an empty answer to {pathAndQuery.Split('?')[0]}.");
    }

    private async Task<string> GetTokenAsync(CancellationToken ct)
    {
        if (_token is not null)
            return _token;

        using var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["client_id"] = clientId,
            ["client_secret"] = clientSecret,
            ["grant_type"] = "client_credentials",
        });
        using var response = await http.PostAsync("https://id.twitch.tv/oauth2/token", form, ct);
        if (!response.IsSuccessStatusCode)
            throw new TwitchException($"Twitch refused the client ID and secret ({(int)response.StatusCode}). Check TWITCH_CLIENT_ID and TWITCH_CLIENT_SECRET.");

        var token = await response.Content.ReadFromJsonAsync<TokenResponse>(Json, ct);
        return _token = token?.AccessToken ?? throw new TwitchException("Twitch sent no access token.");
    }

    private sealed record TokenResponse(string AccessToken);

    private sealed record HelixResponse<T>(IReadOnlyList<T> Data);
}

/// <summary>A Twitch game category. <see cref="BoxArtUrl"/> has <c>{width}</c> and <c>{height}</c> to fill in.</summary>
public sealed record TwitchCategory(string Id, string Name, [property: JsonPropertyName("box_art_url")] string BoxArtUrl);

public sealed class TwitchException(string message) : Exception(message);
