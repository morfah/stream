using System.Net.Http.Json;
using System.Text.Json;
using System.Xml;

namespace MorfStudios.StreamSite.YouTube;

/// <summary>
/// The YouTube Data API with an API key, which only reads public data. A build costs about 45 of
/// the 10,000 free units a day: one per call, and calls list 50 videos each.
/// </summary>
public sealed class YouTubeClient(HttpClient http, string apiKey)
{
    private const int MaxPerRequest = 50;

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    /// <summary>Every public video on the channel, newest upload first.</summary>
    /// <param name="handle">The channel's handle, like <c>@MorfahVODs</c>.</param>
    public async Task<IReadOnlyList<YouTubeVideo>> GetChannelVideosAsync(string handle, CancellationToken ct)
    {
        var channels = await GetAsync<ListResponse<Channel>>($"channels?part=contentDetails&forHandle={Uri.EscapeDataString(handle)}", ct);
        var uploads = channels.Items.FirstOrDefault()?.ContentDetails.RelatedPlaylists.Uploads
            ?? throw new YouTubeException($"YouTube has no channel {handle}.");

        var ids = new List<string>();
        string? page = null;
        do
        {
            var query = $"playlistItems?part=contentDetails&maxResults={MaxPerRequest}&playlistId={uploads}";
            var items = await GetAsync<ListResponse<PlaylistItem>>(page is null ? query : $"{query}&pageToken={page}", ct);
            ids.AddRange(items.Items.Select(i => i.ContentDetails.VideoId));
            page = items.NextPageToken;
        }
        while (page is not null);

        var videos = new List<YouTubeVideo>();
        foreach (var chunk in ids.Distinct().Chunk(MaxPerRequest))
        {
            var response = await GetAsync<ListResponse<Video>>($"videos?part=snippet,contentDetails,status&id={string.Join(',', chunk)}", ct);
            videos.AddRange(response.Items.Select(v => new YouTubeVideo(
                v.Id,
                v.Snippet.Title,
                v.Snippet.PublishedAt,
                XmlConvert.ToTimeSpan(v.ContentDetails.Duration),
                v.Snippet.Tags ?? [],
                v.Status.Embeddable)));
        }
        return videos;
    }

    private async Task<T> GetAsync<T>(string pathAndQuery, CancellationToken ct)
    {
        var url = $"https://www.googleapis.com/youtube/v3/{pathAndQuery}&key={Uri.EscapeDataString(apiKey)}";
        using var response = await http.GetAsync(url, ct);
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadFromJsonAsync<ErrorResponse>(Json, ct);
            throw new YouTubeException($"YouTube answered {(int)response.StatusCode} to {pathAndQuery.Split('?')[0]}: {error?.Error.Message ?? "no reason given"}");
        }
        return await response.Content.ReadFromJsonAsync<T>(Json, ct)
            ?? throw new YouTubeException($"YouTube sent an empty answer to {pathAndQuery.Split('?')[0]}.");
    }

    private sealed record ListResponse<T>(IReadOnlyList<T> Items, string? NextPageToken);
    private sealed record Channel(ChannelDetails ContentDetails);
    private sealed record ChannelDetails(RelatedPlaylists RelatedPlaylists);
    private sealed record RelatedPlaylists(string Uploads);
    private sealed record PlaylistItem(PlaylistItemDetails ContentDetails);
    private sealed record PlaylistItemDetails(string VideoId);
    private sealed record Video(string Id, VideoSnippet Snippet, VideoDetails ContentDetails, VideoStatus Status);
    private sealed record VideoSnippet(string Title, DateTimeOffset PublishedAt, IReadOnlyList<string>? Tags);
    private sealed record VideoDetails(string Duration);
    private sealed record VideoStatus(bool Embeddable);
    private sealed record ErrorResponse(ErrorDetails Error);
    private sealed record ErrorDetails(string Message);
}

/// <param name="Embeddable">False when I've turned off embedding, so it can only be watched on YouTube.</param>
public sealed record YouTubeVideo(string Id, string Title, DateTimeOffset PublishedAt, TimeSpan Duration, IReadOnlyList<string> Tags, bool Embeddable);

public sealed class YouTubeException(string message) : Exception(message);
