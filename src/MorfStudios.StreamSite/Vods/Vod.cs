using System.Globalization;
using MorfStudios.StreamSite.YouTube;

namespace MorfStudios.StreamSite.Vods;

/// <summary>A stream on my YouTube VOD channel.</summary>
/// <param name="Title">The title on YouTube.</param>
/// <param name="ShortTitle">The title without its date, for showing next to the date. See <see cref="VodTitles.Shorten"/>.</param>
/// <param name="Date">The day I streamed it: from the title, otherwise the upload day.</param>
/// <param name="Order">1 or 2 for the A and B streams of a day, otherwise 0.</param>
/// <param name="DateFromTitle">False when the title has no date, or one that can't be right, and <paramref name="Date"/> is the upload day.</param>
public sealed record Vod(
    string Id,
    string Title,
    string ShortTitle,
    DateOnly Date,
    int Order,
    bool DateFromTitle,
    TimeSpan Duration,
    IReadOnlyList<string> Tags,
    bool Embeddable,
    DateTimeOffset UploadedAt)
{
    public string WatchUrl => $"https://www.youtube.com/watch?v={Id}";

    /// <summary>480x360 with black bars, which the page crops to 16:9. Exists for every video.</summary>
    public string ThumbnailUrl => $"https://i.ytimg.com/vi/{Id}/hqdefault.jpg";

    /// <summary>Like 3:45:51, or 45:12 under an hour.</summary>
    public string Length => Duration.TotalHours >= 1
        ? $"{(int)Duration.TotalHours}:{Duration.Minutes:00}:{Duration.Seconds:00}"
        : $"{Duration.Minutes}:{Duration.Seconds:00}";

    /// <summary>Like "Tue 14 Oct 2025".</summary>
    public string LongDate => Date.ToString("ddd d MMM yyyy", CultureInfo.InvariantCulture);

    /// <summary>Newest stream first. Same-day streams by their A and B, then by upload time.</summary>
    public static IOrderedEnumerable<Vod> NewestFirst(IEnumerable<Vod> vods) => vods
        .OrderByDescending(v => v.Date)
        .ThenByDescending(v => v.Order)
        .ThenByDescending(v => v.UploadedAt);

    /// <summary>My first VODs are from 2011, so an earlier date in a title is a typo, like 2002 for 2020.</summary>
    public const int FirstYear = 2010;

    public static Vod From(YouTubeVideo video)
    {
        var uploaded = DateOnly.FromDateTime(video.PublishedAt.UtcDateTime);
        var date = VodTitles.ParseDate(video.Title);
        // A stream can't be uploaded before it happened, so a later date is a typo too.
        if (date is { } d && (d.Date.Year < FirstYear || d.Date > uploaded.AddDays(2)))
            date = null;

        return new Vod(
            video.Id,
            video.Title,
            VodTitles.Shorten(video.Title),
            date?.Date ?? uploaded,
            date?.Order ?? 0,
            date is not null,
            video.Duration,
            video.Tags,
            video.Embeddable,
            video.PublishedAt);
    }
}
