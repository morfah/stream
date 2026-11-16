using System.Globalization;
using System.Text;
using MorfStudios.StreamSite.Data;
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
            GameList.Other => (PlayedPage.OtherFile, "← All other streams"),
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

        // A game played more than once, like Valheim solo and then co-op, has a list for each playthrough.
        var split = game.Playthroughs.Count > 1 && (game.Vods.Count > 0 || game.MissingVods.Count > 0);
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
                  <div class="section-head"><h2>VODs</h2><span>{(split ? "by playthrough, oldest first" : "oldest first")}</span></div>

            """);

        if (!split)
        {
            body.Append(VodList(game, game.Vods, game.MissingVods, "No VODs of this game yet."));
        }
        else
        {
            // Each playthrough has its own list, so "Next part" stops at its end.
            foreach (var playthrough in game.Playthroughs)
            {
                var (vods, missing) = (game.VodsOf(playthrough), game.MissingVodsOf(playthrough));
                body.Append($"""
                          <div class="playthrough-head"><h3>{Who(playthrough)}</h3><span>{Outcome(playthrough)} · {GameCards.VodsNote(vods, missing)}</span></div>

                    """);
                body.Append(VodList(game, vods, missing, "No VODs of this playthrough."));
            }
        }

        body.Append($"""
                </div>

            {VodCards.PlayerDialog}
            """);

        return Layout.Page(page, body.ToString());
    }

    /// <summary>The VOD cards, with the missing VODs in date order between the others, after those of the same day.</summary>
    /// <param name="empty">Shown when there are none.</param>
    private static string VodList(Game game, IReadOnlyList<Vod> vods, IReadOnlyList<MissingVod> missing, string empty)
    {
        if (vods.Count == 0 && missing.Count == 0)
        {
            return $"""
                      <p class="placeholder">{empty}</p>

                """;
        }

        var cards = vods.Select((vod, i) => (vod.Date, Sort: i, Html: VodCards.Card(vod, VodCards.Milestones([(game, game.MilestonesOf(vod))]))))
            .Concat(missing.Select((lost, i) => (lost.Date, Sort: vods.Count + i,
                Html: VodCards.MissingCard(lost, game, VodCards.Milestones([(game, game.MilestonesOf(lost))])))))
            .OrderBy(card => card.Date)
            .ThenBy(card => card.Sort);
        return """
                      <ol class="vods" data-sequence>

                """ + string.Concat(cards.Select(card => card.Html)) + """
                      </ol>

                """;
    }

    /// <summary>"Solo playthrough", or "Co-op with franticalle and Nyakuzaa".</summary>
    private static string Who(Playthrough playthrough) =>
        playthrough.Coop.Count > 0 ? $"Co-op with {And(playthrough.Coop.Select(p => E(p.Name)))}" : "Solo playthrough";

    /// <summary>Like "Finished on 5 Apr 2021", "Finished at 100% on 2 Feb 2024 · Hardest difficulty", or "Not finished yet".</summary>
    private static string Outcome(Playthrough playthrough)
    {
        if (playthrough.Finish is not { } finish)
            return "Not finished yet";
        var outcome = finish.HundredPercent == finish.Date ? $"Finished at 100% on {Days([finish.Date])}"
            : finish.HundredPercent is { } hundred ? $"Finished on {Days([finish.Date])} · 100% on {Days([hundred])}"
            : $"Finished on {Days([finish.Date])}";
        return finish.Difficulty is { } difficulty ? $"{outcome} · {E(difficulty)} difficulty" : outcome;
    }

    /// <summary>
    /// Like "Finished on stream on 3 Dec 2022 · 100% on 28 Jan 2023", "Finished on stream on 24 Feb 2023 and 2 Feb 2024 · 100% on
    /// 2 Feb 2024", "Finished on stream at 100% on 1 Dec 2021", or "Last played on stream in 2025 · not finished yet".
    /// </summary>
    private static string Status(Game game)
    {
        var lastPlayed = game.LastPlayed?.Year.ToString(CultureInfo.InvariantCulture);
        if (game.List == GameList.Multiplayer)
            return $"Multiplayer · last played on stream in {lastPlayed}";
        if (game.List == GameList.Unfinished)
            return $"Last played on stream in {lastPlayed} · not finished yet";
        if (game.List == GameList.Other)
            return $"Other streams · last streamed in {lastPlayed}";

        var finished = Days(game.Finishes.Select(f => f.Date));
        var hundred = game.Finishes.Select(f => f.HundredPercent).OfType<DateOnly>().ToList();
        if (hundred.Count == 0)
            return $"Finished on stream on {finished}";
        // "Finished on stream at 100% on 1 Dec 2021" when I got both the same day.
        return game.Finishes.All(f => f.HundredPercent == f.Date)
            ? $"Finished on stream at 100% on {finished}"
            : $"Finished on stream on {finished} · 100% on {Days(hundred)}";
    }

    /// <summary>Like "20 Apr 2022", or "24 Feb 2023 and 2 Feb 2024".</summary>
    private static string Days(IEnumerable<DateOnly> days) =>
        And(days.Distinct().Order().Select(d => d.ToString("d MMM yyyy", CultureInfo.InvariantCulture)));

    private static string Badges(Game game) => string.Concat(
        game.Finishes.Any(f => f.HundredPercent is not null) ? """<span class="badge gold">🏆 100%</span>""" : "",
        game.Coop.Count > 0 ? $"""<span class="badge coop">Co-op with {And(game.Coop.Select(Partner))}</span>""" : "",
        string.Concat(game.Finishes.Select(f => f.Difficulty).OfType<string>().Distinct().Select(difficulty =>
            $"""<span class="badge">💀 {E(difficulty)} difficulty</span>""")));

    /// <summary>A link to their Twitch channel, or only their name when they don't have one.</summary>
    private static string Partner(CoopPartner partner) => partner.TwitchUrl is { } url
        ? $"""<a href="{E(url)}" title="{E(partner.Name)} on Twitch">{E(partner.Name)}</a>"""
        : E(partner.Name);

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
