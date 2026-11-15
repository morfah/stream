using System.Text;
using MorfStudios.StreamSite.Data;
using MorfStudios.StreamSite.Games;
using static MorfStudios.StreamSite.Pages.Html;

namespace MorfStudios.StreamSite.Pages;

/// <summary>The home page: every game I finished on stream, grouped by year, newest first.</summary>
public static class FinishedPage
{
    public const string File = "index.html";

    /// <param name="games">The finished list, one line per finish.</param>
    /// <param name="pages">The game page for each finish, keyed with <see cref="Names.Key"/>.</param>
    public static string Render(IReadOnlyList<FinishedGame> games, IReadOnlyDictionary<string, Game> pages, DateOnly updated)
    {
        var years = games.Select(g => g.Year).Distinct().OrderDescending().ToList();
        var body = new StringBuilder();

        body.Append($"""
                <section class="hero wrap">
                  <p class="eyebrow">Morfah on Twitch</p>
                  <h1>Games finished <span class="grad-text">on stream</span></h1>
                  <p class="lede">Every game I've played to the end live on stream.</p>
                  <dl class="stats">
                    <div><dt>games finished</dt><dd>{games.Count}</dd></div>
                    <div><dt>at 100% 🏆</dt><dd>{games.Count(g => g.HundredPercent)}</dd></div>
                    <div><dt>years of streams</dt><dd>{years.Count}</dd></div>
                  </dl>
                </section>

                <div class="toolbar">
                  <div class="wrap">
                    <nav class="years" aria-label="Years">{string.Concat(years.Select(y => $"""<a href="#y{y}">{y}</a>"""))}</nav>
                    <div class="filters">
                      <button class="chip" type="button" data-filter="all" aria-pressed="true">All</button>
                      <button class="chip" type="button" data-filter="hundred" aria-pressed="false">100% 🏆</button>
                      <button class="chip" type="button" data-filter="coop" aria-pressed="false">Co-op</button>
                      <input class="search" id="search" type="search" placeholder="Find a game" aria-label="Find a game">
                    </div>
                  </div>
                </div>

                <div class="wrap">

            """);

        foreach (var year in years)
        {
            var inYear = games.Where(g => g.Year == year).ToList();
            body.Append($"""
                    <section class="year" id="y{year}">
                      <div class="year-head"><h2 class="grad-text">{year}</h2><span class="count">{Plural(inYear.Count, "game")}</span></div>
                      <ol class="grid">

                """);
            foreach (var game in inYear)
                body.Append(Card(game, pages[Names.Key(game.Name)]));
            body.Append("""
                      </ol>
                    </section>

                """);
        }

        body.Append("""
                </div>
                <p class="empty wrap" id="empty" hidden>No game matches that.</p>
            """);

        var page = new PageInfo(File, "./", "Games finished on stream · Morfah", "Every game Morfah has finished live on Twitch, with the VODs.", updated) { Script = "site.js" };
        return Layout.Page(page, body.ToString());
    }

    /// <param name="game">One finish. A game finished twice has a card for each.</param>
    /// <param name="page">The game's page, with its cover and VODs.</param>
    private static string Card(FinishedGame game, Game page)
    {
        var cover = page.Cover?.Small;
        var art = cover is null
            ? $"<span>{E(game.Name)}</span>"
            : $"""<img src="{E(cover)}" alt="" loading="lazy" width="{Twitch.CoverFinder.Width}" height="{Twitch.CoverFinder.Height}">""";

        var badges = string.Concat(
            game.HundredPercent ? """<span class="badge gold">🏆 100%</span>""" : "",
            game.Coop is not null ? """<span class="badge">Co-op</span>""" : "",
            game.Difficulty is not null ? $"""<span class="badge" title="{E(game.Difficulty)} difficulty">💀</span>""" : "");

        var notes = string.Join(" · ", new[]
        {
            page.Vods.Count > 0 ? Plural(page.Vods.Count, "VOD") : page.MissingVods.Count > 0 ? "VOD missing 😢" : "No VODs",
            game.Coop is { } coop ? $"Co-op with {E(coop)}" : null,
            game.Difficulty is { } difficulty ? $"{E(difficulty)} difficulty" : null,
        }.OfType<string>());

        var flags = (game.HundredPercent ? " data-hundred" : "") + (game.Coop is not null ? " data-coop" : "");
        var badgeList = badges.Length > 0 ? $"""<div class="badges">{badges}</div>""" : "";
        var note = notes.Length > 0 ? $"""{"\n"}            <p class="note">{notes}</p>""" : "";

        return $"""
                      <li class="card" data-name="{E(Names.Key(game.Name))}"{flags}>
                        <a href="{E(page.Path)}">
                          <div class="cover{(cover is null ? " no-art" : "")}">{art}{badgeList}</div>
                          <h3>{E(game.Name)}</h3>{note}
                        </a>
                      </li>

            """;
    }
}
