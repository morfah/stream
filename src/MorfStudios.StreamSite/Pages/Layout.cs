using System.Globalization;

namespace MorfStudios.StreamSite.Pages;

/// <summary>The parts every page shares: head, top bar with the menu, and footer.</summary>
public static class Layout
{
    public const string SiteUrl = "https://morfah.github.io/stream/";

    /// <summary>The menu. Later steps add Unfinished, Multiplayer and VOD archive.</summary>
    private static readonly (string Href, string Label)[] Menu =
    [
        ("./", "Finished"),
    ];

    private const string TwitchIcon = """<svg viewBox="0 0 24 24" aria-hidden="true"><path d="M11.571 4.714h1.715v5.143H11.57zm4.715 0H18v5.143h-1.714zM6 0 1.714 4.286v15.428h5.143V24l4.286-4.286h3.428L22.286 12V0zm14.571 11.143-3.428 3.428h-3.429l-3 3v-3H6.857V1.714h13.714Z"/></svg>""";
    private const string YouTubeIcon = """<svg viewBox="0 0 24 24" aria-hidden="true"><path d="M23.498 6.186a3.016 3.016 0 0 0-2.122-2.136C19.505 3.545 12 3.545 12 3.545s-7.505 0-9.377.505A3.017 3.017 0 0 0 .502 6.186C0 8.07 0 12 0 12s0 3.93.502 5.814a3.016 3.016 0 0 0 2.122 2.136c1.871.505 9.376.505 9.376.505s7.505 0 9.377-.505a3.015 3.015 0 0 0 2.122-2.136C24 15.93 24 12 24 12s0-3.93-.502-5.814zM9.545 15.568V8.432L15.818 12l-6.273 3.568z"/></svg>""";

    /// <param name="menuHref">The menu item this page belongs to, like <c>./</c>.</param>
    /// <param name="body">The page's own HTML, between the top bar and the footer.</param>
    public static string Page(string menuHref, string title, string description, string body, DateOnly updated, string? script = null)
    {
        var menu = string.Join("\n        ", Menu.Select(item =>
            $"""<a href="{item.Href}"{(item.Href == menuHref ? " aria-current=\"page\"" : "")}>{Html.E(item.Label)}</a>"""));
        var scriptTag = script is null ? "" : $"""
              <script src="{script}"></script>
            """;

        return $"""
            <!doctype html>
            <html lang="en">
            <head>
              <meta charset="utf-8">
              <meta name="viewport" content="width=device-width, initial-scale=1">
              <title>{Html.E(title)}</title>
              <meta name="description" content="{Html.E(description)}">
              <meta property="og:type" content="website">
              <meta property="og:title" content="{Html.E(title)}">
              <meta property="og:description" content="{Html.E(description)}">
              <meta property="og:image" content="{SiteUrl}logo.png">
              <link rel="icon" href="favicon.ico">
              <link rel="stylesheet" href="style.css">
            </head>
            <body>
              <header class="topbar">
                <div class="wrap">
                  <a class="brand" href="./"><img src="logo.png" alt="Morfah" width="339" height="359"></a>
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
                  Updated {updated.ToString("d MMM yyyy", CultureInfo.InvariantCulture)}.
                </div>
              </footer>
            {scriptTag}
            </body>
            </html>

            """;
    }
}
