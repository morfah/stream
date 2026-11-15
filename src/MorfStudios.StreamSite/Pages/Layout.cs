using System.Globalization;

namespace MorfStudios.StreamSite.Pages;

/// <param name="Path">The file from the site's root, like <c>index.html</c> or <c>games/elden-ring/index.html</c>.</param>
/// <param name="MenuHref">The menu item the page belongs to, like <c>./</c>.</param>
/// <param name="Image">The picture for link previews, from the site's root. The logo when null.</param>
/// <param name="Script">A script from the site's root, like <c>site.js</c>.</param>
/// <param name="Hidden">Not for search engines, like the review page.</param>
public sealed record PageInfo(string Path, string MenuHref, string Title, string Description, DateOnly Updated)
{
    public string? Image { get; init; }
    public string? Script { get; init; }
    public bool Hidden { get; init; }

    /// <summary>The way back to the site's root from this page, like <c>../../</c>.</summary>
    public string Root => string.Concat(Enumerable.Repeat("../", Path.Count(c => c == '/')));

    /// <summary>A link from this page to a path from the site's root.</summary>
    public string Link(string path) => path == "./" ? (Root.Length > 0 ? Root : "./") : Root + path;
}

/// <summary>The parts every page shares: head, top bar with the menu, and footer.</summary>
public static class Layout
{
    public const string SiteUrl = "https://morfah.github.io/stream/";

    private static readonly (string Href, string Label)[] Menu =
    [
        ("./", "Finished"),
        (PlayedPage.UnfinishedFile, "Unfinished"),
        (PlayedPage.MultiplayerFile, "Multiplayer"),
        (PlayedPage.OtherFile, "Other"),
        (VodsPage.File, "VOD archive"),
    ];

    private const string TwitchIcon = """<svg viewBox="0 0 24 24" aria-hidden="true"><path d="M11.571 4.714h1.715v5.143H11.57zm4.715 0H18v5.143h-1.714zM6 0 1.714 4.286v15.428h5.143V24l4.286-4.286h3.428L22.286 12V0zm14.571 11.143-3.428 3.428h-3.429l-3 3v-3H6.857V1.714h13.714Z"/></svg>""";
    private const string YouTubeIcon = """<svg viewBox="0 0 24 24" aria-hidden="true"><path d="M23.498 6.186a3.016 3.016 0 0 0-2.122-2.136C19.505 3.545 12 3.545 12 3.545s-7.505 0-9.377.505A3.017 3.017 0 0 0 .502 6.186C0 8.07 0 12 0 12s0 3.93.502 5.814a3.016 3.016 0 0 0 2.122 2.136c1.871.505 9.376.505 9.376.505s7.505 0 9.377-.505a3.015 3.015 0 0 0 2.122-2.136C24 15.93 24 12 24 12s0-3.93-.502-5.814zM9.545 15.568V8.432L15.818 12l-6.273 3.568z"/></svg>""";

    /// <param name="body">The page's own HTML, between the top bar and the footer.</param>
    public static string Page(PageInfo page, string body)
    {
        var menu = string.Join("\n        ", Menu.Select(item =>
            $"""<a href="{page.Link(item.Href)}"{(item.Href == page.MenuHref ? " aria-current=\"page\"" : "")}>{Html.E(item.Label)}</a>"""));
        var url = SiteUrl + (page.Path.EndsWith("index.html") ? page.Path[..^"index.html".Length] : page.Path);
        var robots = page.Hidden ? """

              <meta name="robots" content="noindex">
            """.TrimEnd() : "";
        var script = page.Script is null ? "" : $"""

              <script src="{page.Link(page.Script)}"></script>
            """.TrimEnd();

        return $"""
            <!doctype html>
            <html lang="en">
            <head>
              <meta charset="utf-8">
              <meta name="viewport" content="width=device-width, initial-scale=1">
              <title>{Html.E(page.Title)}</title>
              <meta name="description" content="{Html.E(page.Description)}">{robots}
              <meta property="og:type" content="website">
              <meta property="og:url" content="{Html.E(url)}">
              <meta property="og:title" content="{Html.E(page.Title)}">
              <meta property="og:description" content="{Html.E(page.Description)}">
              <meta property="og:image" content="{Html.E(SiteUrl + (page.Image ?? "logo.png"))}">
              <link rel="icon" href="{page.Link("favicon.ico")}">
              <link rel="stylesheet" href="{page.Link("style.css")}">
            </head>
            <body>
              <header class="topbar">
                <div class="wrap">
                  <a class="brand" href="{page.Link("./")}"><img src="{page.Link("logo.png")}" alt="Morfah" width="339" height="359"></a>
                  <nav class="nav">
                    {menu}
                  </nav>
                  <div class="social">
                    <a class="twitch" href="https://www.twitch.tv/morfah" aria-label="Morfah on Twitch" title="Twitch">{TwitchIcon}</a>
                    <a class="youtube" href="https://www.youtube.com/@MorfahVODs" aria-label="Morfah VODs on YouTube" title="VODs on YouTube">{YouTubeIcon}</a>
                  </div>
                </div>
              </header>

              <main>
            {body}
              </main>

              <footer class="footer">
                <div class="wrap">
                  Cover art from Twitch and IGDB. VODs on <a href="https://www.youtube.com/@MorfahVODs">YouTube</a>.
                  Updated {page.Updated.ToString("d MMM yyyy", CultureInfo.InvariantCulture)}.
                </div>
              </footer>{script}
            </body>
            </html>

            """;
    }
}
