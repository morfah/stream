using System.Globalization;
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
        var body = new StringBuilder($"""
                <section class="hero wrap">
                  <p class="eyebrow">Morfah on Twitch</p>
                  <h1>Games finished <span class="grad-text">on stream</span></h1>
                  <p class="lede">Every game I've played to the end live on stream.</p>
                  <dl class="stats">
                    <div><dt>games finished</dt><dd>{games.Count}</dd></div>
                    <div><dt>at 100% 🏆</dt><dd>{games.Count(g => g.HundredPercent is not null)}</dd></div>
                    <div><dt>years of streams</dt><dd>{years.Count}</dd></div>
                  </dl>
                </section>


            """);

        body.Append(GameCards.Toolbar(years, """

                      <button class="chip" type="button" data-filter="all" aria-pressed="true">All</button>
                      <button class="chip" type="button" data-filter="hundred" aria-pressed="false">100% 🏆</button>
                      <button class="chip" type="button" data-filter="coop" aria-pressed="false">Co-op</button>
            """));
        body.Append("""
                <div class="wrap">

            """);
        foreach (var year in years)
            body.Append(GameCards.Year(year, games.Where(g => g.Year == year).Select(g => Card(g, pages[Names.Key(g.Name)])).ToList()));
        body.Append($"""
                </div>
            {GameCards.Empty()}
            """);

        var page = new PageInfo(File, "./", "Games finished on stream · Morfah", "Every game Morfah has finished live on Twitch, with the VODs.", updated) { Script = "site.js" };
        return Layout.Page(page, body.ToString());
    }

    /// <param name="game">One finish. A game finished twice has a card for each.</param>
    /// <param name="page">The game's page, with its cover and VODs.</param>
    private static string Card(FinishedGame game, Game page)
    {
        var badges = string.Concat(
            game.HundredPercent is { } hundred ? $"""<span class="badge gold" title="100% on {hundred.ToString("d MMM yyyy", CultureInfo.InvariantCulture)}">🏆 100%</span>""" : "",
            game.Coop is not null ? """<span class="badge">Co-op</span>""" : "",
            game.Difficulty is not null ? $"""<span class="badge" title="{E(game.Difficulty)} difficulty">💀</span>""" : "");
        var notes = new[]
        {
            GameCards.VodsNote(page),
            game.Coop is { } coop ? $"Co-op with {E(coop)}" : null,
            game.Difficulty is { } difficulty ? $"{E(difficulty)} difficulty" : null,
        };
        var flags = (game.HundredPercent is not null ? " data-hundred" : "") + (game.Coop is not null ? " data-coop" : "");
        return GameCards.Card(game.Name, page, badges, notes, flags);
    }
}
