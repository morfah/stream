using System.Diagnostics;
using MorfStudios.StreamSite;
using MorfStudios.StreamSite.Data;
using MorfStudios.StreamSite.Pages;
using MorfStudios.StreamSite.Twitch;

// Builds the site into _site: reads the data files, gets covers from Twitch and writes the pages.
// Run from anywhere in the repo: dotnet run --project src/MorfStudios.StreamSite
// Or look up Twitch categories for data/games.json: dotnet run --project src/MorfStudios.StreamSite -- search "game name"
try
{
    var stopwatch = Stopwatch.StartNew();
    var repoRoot = FindRepoRoot();
    var siteDir = Path.Combine(repoRoot, "_site");
    var keys = Keys.Load(repoRoot);

    using var http = new HttpClient();
    http.DefaultRequestHeaders.UserAgent.ParseAdd("MorfStreamSite/1.0");
    var twitch = keys.HasTwitch ? new TwitchClient(http, keys.TwitchClientId!, keys.TwitchClientSecret!) : null;

    if (args is ["search", var query])
    {
        if (twitch is null)
            throw new DataException($"Searching needs the Twitch keys in {Keys.LocalFile}.");
        foreach (var category in await twitch.SearchAsync(query, CancellationToken.None))
            Console.WriteLine($"{category.Id,-12} {category.Name}");
        return 0;
    }

    var data = DataFiles.Load(repoRoot);
    var report = new Report();

    EmptyFolder(siteDir);
    CopyFolder(Path.Combine(repoRoot, "site"), siteDir);

    var covers = await new CoverFinder(http, twitch, repoRoot, siteDir, report)
        .FindAsync(data.FinishedGames.Select(g => g.Name), data.Games, CancellationToken.None);

    var updated = DateOnly.FromDateTime(DateTime.UtcNow);
    await File.WriteAllTextAsync(Path.Combine(siteDir, FinishedPage.File), FinishedPage.Render(data.FinishedGames, covers, updated));

    report.Print();
    report.WriteGitHubSummary();
    Console.WriteLine();
    Console.WriteLine($"Built the site in {siteDir} in {stopwatch.Elapsed.TotalSeconds:0.0} s.");
    return 0;
}
catch (Exception ex) when (ex is DataException or TwitchException or HttpRequestException)
{
    Console.Error.WriteLine($"The build failed: {ex.Message}");
    return 1;
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
