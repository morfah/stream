using System.Globalization;
using System.Text;
using MorfStudios.StreamSite.Games;
using MorfStudios.StreamSite.Vods;
using static MorfStudios.StreamSite.Pages.Html;

namespace MorfStudios.StreamSite.Pages;

/// <summary>A game's own page: cover, what I did in it, and all its VODs, oldest first.</summary>
public static class GamePage
{
    public static string FileFor(Game game) => game.Path + "index.html";

    public static string Render(Game game, DateOnly updated)
    {
        var status = Status(game);
        var description = game.Vods.Count > 0 ? $"{status}. {Plural(game.Vods.Count, "VOD")} on YouTube." : $"{status}.";
        var (list, back) = game.List switch
        {
            GameList.Unfinished => (PlayedPage.UnfinishedFile, "← All unfinished games"),
            GameList.Multiplayer => (PlayedPage.MultiplayerFile, "← All multiplayer games"),
            _ => ("./", "← All finished games"),
        };
        var page = new PageInfo(FileFor(game), list, $"{game.Name} · Morfah's VODs", description, updated)
        {
            Script = "site.js",
            Image = game.Cover?.Large,
        };

        var cover = game.Cover is { } c
            ? $"""<div class="cover"><img src="{E(page.Link(c.Large))}" alt="{E(game.Name)} cover" width="{Twitch.CoverFinder.LargeWidth}" height="{Twitch.CoverFinder.LargeHeight}"></div>"""
            : $"""<div class="cover no-art"><span>{E(game.Name)}</span></div>""";
        var backdrop = game.Cover is { } b ? $""" style="background-image: url('{E(page.Link(b.Large))}')" """.TrimEnd() : "";
        var first = game.Vods.FirstOrDefault(v => v.Embeddable) ?? game.Vods.FirstOrDefault();
        var watch = first is null ? "" : $"""

                        <div class="actions"><a class="btn primary" href="{E(first.WatchUrl)}" data-play-first>▶ {(game.Vods.Count == 1 ? "Watch the VOD" : "Watch from the start")}</a></div>
            """.TrimEnd();

        var body = new StringBuilder($"""
                <section class="game-hero">
                  <div class="backdrop"{backdrop}></div>
                  <div class="wrap">
                    <a class="back" href="{page.Link(list)}">{back}</a>
                    <div class="game-head">
                      {cover}
                      <div class="game-info">
                        <p class="eyebrow">{E(status)}</p>
                        <h1>{E(game.Name)}</h1>
                        <div class="badges">{Badges(game)}</div>
                        <ul class="facts">{Facts(game)}</ul>{watch}
                      </div>
                    </div>
                  </div>
                </section>

                <div class="wrap">
                  <div class="section-head"><h2>VODs</h2><span>oldest first</span></div>

            """);

        if (game.Vods.Count == 0 && game.MissingVods.Count == 0)
        {
            body.Append("""
                      <p class="placeholder">No VODs of this game yet.</p>

                """);
        }
        else
        {
            body.Append("""
                      <ol class="vods" data-sequence>

                """);
            // Missing VODs in date order between the others, after those of the same day.
            var cards = game.Vods.Select((vod, i) => (vod.Date, Sort: i, Html: VodCards.Card(vod)))
                .Concat(game.MissingVods.Select((missing, i) => (missing.Date, Sort: game.Vods.Count + i, Html: VodCards.MissingCard(missing, game))))
                .OrderBy(card => card.Date)
                .ThenBy(card => card.Sort);
            foreach (var card in cards)
                body.Append(card.Html);
            body.Append("""
                      </ol>

                """);
        }

        body.Append($"""
                </div>

            {VodCards.PlayerDialog}
            """);

        return Layout.Page(page, body.ToString());
    }

    /// <summary>Like "Finished on stream in 2022 and 2023", or "Last played on stream in 2025 · not finished yet".</summary>
    private static string Status(Game game)
    {
        var lastPlayed = game.LastPlayed?.Year.ToString(CultureInfo.InvariantCulture);
        if (game.List == GameList.Multiplayer)
            return $"Multiplayer · last played on stream in {lastPlayed}";
        if (game.List == GameList.Unfinished)
            return $"Last played on stream in {lastPlayed} · not finished yet";

        var years = game.Finishes.Select(f => f.Year).Distinct().Order().Select(y => y.ToString(CultureInfo.InvariantCulture)).ToArray();
        var list = years.Length == 1 ? years[0] : string.Join(", ", years[..^1]) + " and " + years[^1];
        return $"Finished on stream in {list}";
    }

    private static string Badges(Game game) => string.Concat(
        game.Finishes.Any(f => f.HundredPercent) ? """<span class="badge gold">🏆 100%</span>""" : "",
        string.Concat(game.Finishes.Select(f => f.Coop).OfType<string>().Distinct().Select(coop =>
            $"""<a class="badge" href="https://www.twitch.tv/{Uri.EscapeDataString(coop)}">Co-op with {E(coop)}</a>""")),
        string.Concat(game.Finishes.Select(f => f.Difficulty).OfType<string>().Distinct().Select(difficulty =>
            $"""<span class="badge">💀 {E(difficulty)} difficulty</span>""")));

    /// <summary>How many VODs, how long, and when, and how many are missing.</summary>
    private static string Facts(Game game)
    {
        var missing = game.MissingVods.Count switch
        {
            0 => "",
            1 => "<li><strong>1</strong> missing VOD 😢</li>",
            var n => $"<li><strong>{n}</strong> missing VODs 😢</li>",
        };
        var vods = game.Vods;
        if (vods.Count == 0)
            return missing.Length > 0 ? missing : "<li>No VODs yet</li>";

        var total = TimeSpan.FromTicks(vods.Sum(v => v.Duration.Ticks));
        var length = total.TotalHours >= 1
            ? $"<strong>{Math.Round(total.TotalHours).ToString(CultureInfo.InvariantCulture)}</strong> hours"
            : $"<strong>{(int)total.TotalMinutes}</strong> minutes";

        var (first, last) = (vods[0].Date, vods[^1].Date);
        var when = first == last ? vods[0].LongDate
            : first.Year == last.Year ? $"{first.ToString("d MMM", CultureInfo.InvariantCulture)} – {last.ToString("d MMM yyyy", CultureInfo.InvariantCulture)}"
            : $"{first.ToString("d MMM yyyy", CultureInfo.InvariantCulture)} – {last.ToString("d MMM yyyy", CultureInfo.InvariantCulture)}";

        return $"<li><strong>{vods.Count}</strong> {(vods.Count == 1 ? "VOD" : "VODs")}</li><li>{length}</li><li>{when}</li>{missing}";
    }
}
