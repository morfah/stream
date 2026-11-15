using System.Diagnostics;
using MorfStudios.StreamSite;
using MorfStudios.StreamSite.Data;
using MorfStudios.StreamSite.Pages;
using MorfStudios.StreamSite.Twitch;
using MorfStudios.StreamSite.Vods;
using MorfStudios.StreamSite.YouTube;

// Builds the site into _site: reads the data files, gets covers from Twitch and VODs from YouTube, and writes the pages.
// Run from anywhere in the repo: dotnet run --project src/MorfStudios.StreamSite
// Helpers for the data files, after "--":
//   search "game name"   Twitch categories and their IDs, for data/games.json.
//   vods ["words"]       My VODs with their IDs, stream dates, upload dates, lengths and titles, optionally only those with the words.
const string Channel = "@MorfahVODs";
var ct = CancellationToken.None;

try
{
    var stopwatch = Stopwatch.StartNew();
    var repoRoot = FindRepoRoot();
    var siteDir = Path.Combine(repoRoot, "_site");
    var keys = Keys.Load(repoRoot);

    using var http = new HttpClient();
    http.DefaultRequestHeaders.UserAgent.ParseAdd("MorfStreamSite/1.0");
    var twitch = keys.HasTwitch ? new TwitchClient(http, keys.TwitchClientId!, keys.TwitchClientSecret!) : null;
    var youTube = keys.YouTubeApiKey is { } apiKey ? new YouTubeClient(http, apiKey) : null;

    if (args is ["search", var query])
    {
        if (twitch is null)
            throw new DataException($"Searching needs the Twitch keys in {Keys.LocalFile}.");
        foreach (var category in await twitch.SearchAsync(query, ct))
            Console.WriteLine($"{category.Id,-12} {category.Name}");
        return 0;
    }

    if (args is ["vods", .. var words])
    {
        if (youTube is null)
            throw new DataException($"Listing VODs needs the YouTube key in {Keys.LocalFile}.");
        var filter = Names.Key(string.Join(' ', words));
        var all = (await youTube.GetChannelVideosAsync(Channel, ct)).Select(Vod.From);
        foreach (var vod in Vod.NewestFirst(all).Where(v => Names.Key(v.Title).Contains(filter)))
            Console.WriteLine($"{vod.Id}  {vod.Date:yyyy-MM-dd}  uploaded {vod.UploadedAt.UtcDateTime:yyyy-MM-dd HH:mm}  {vod.Length,8}  {vod.Title}");
        return 0;
    }

    var data = DataFiles.Load(repoRoot);
    var report = new Report();

    EmptyFolder(siteDir);
    CopyFolder(Path.Combine(repoRoot, "site"), siteDir);

    var covers = await new CoverFinder(http, twitch, repoRoot, siteDir, report)
        .FindAsync(data.FinishedGames.Select(g => g.Name), data.Games, ct);
    var vods = await LoadVodsAsync(youTube, report);

    var updated = DateOnly.FromDateTime(DateTime.UtcNow);
    await File.WriteAllTextAsync(Path.Combine(siteDir, FinishedPage.File), FinishedPage.Render(data.FinishedGames, covers, updated));
    await File.WriteAllTextAsync(Path.Combine(siteDir, VodsPage.File), VodsPage.Render(vods, updated));

    report.Print();
    report.WriteGitHubSummary();
    Console.WriteLine();
    Console.WriteLine($"Built the site in {siteDir} in {stopwatch.Elapsed.TotalSeconds:0.0} s.");
    return 0;
}
catch (Exception ex) when (ex is DataException or TwitchException or YouTubeException or HttpRequestException)
{
    Console.Error.WriteLine($"The build failed: {ex.Message}");
    return 1;
}

async Task<IReadOnlyList<Vod>> LoadVodsAsync(YouTubeClient? youTube, Report report)
{
    if (youTube is null)
    {
        report.Add("VODs", [$"No YouTube key, so the VOD archive is empty. Fill in {Keys.LocalFile} to get the VODs."]);
        return [];
    }

    var vods = Vod.NewestFirst((await youTube.GetChannelVideosAsync(Channel, ct)).Select(Vod.From)).ToList();
    report.Add($"No date in the title, or one that can't be right (before {Vod.FirstYear} or after the upload), so the upload day is used. Fix the title on YouTube:",
        vods.Where(v => !v.DateFromTitle).Select(v => $"{v.Title} ({v.WatchUrl}, uploaded {v.Date:yyyy-MM-dd})"));
    report.Add("Can't be played on the site, because embedding is off on YouTube. They open on YouTube instead:",
        vods.Where(v => !v.Embeddable).Select(v => $"{v.Title} ({v.WatchUrl})"));
    report.Add("VODs", [$"{vods.Count} VODs on {Channel}."]);
    return vods;
}

static string FindRepoRoot()
{
    for (var dir = new DirectoryInfo(Environment.CurrentDirectory); dir is not null; dir = dir.Parent)
    {
        if (File.Exists(Path.Combine(dir.FullName, "data", "finished-games.json")))
            return dir.FullName;
    }
    throw new DataException("Can't find the repo. Run the build from the repo's folder.");
}

// Empties the folder instead of deleting it, so a preview server serving it keeps working.
static void EmptyFolder(string path)
{
    var folder = Directory.CreateDirectory(path);
    foreach (var file in folder.EnumerateFiles())
        file.Delete();
    foreach (var subfolder in folder.EnumerateDirectories())
        subfolder.Delete(recursive: true);
}

static void CopyFolder(string from, string to)
{
    foreach (var source in Directory.EnumerateFiles(from, "*", SearchOption.AllDirectories))
    {
        var target = Path.Combine(to, Path.GetRelativePath(from, source));
        Directory.CreateDirectory(Path.GetDirectoryName(target)!);
        File.Copy(source, target, overwrite: true);
    }
}
