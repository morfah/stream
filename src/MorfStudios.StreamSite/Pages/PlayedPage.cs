using System.Globalization;
using System.Text;
using MorfStudios.StreamSite.Data;
using MorfStudios.StreamSite.Games;
using MorfStudios.StreamSite.Vods;

namespace MorfStudios.StreamSite.Pages;

/// <summary>
/// Unfinished, Multiplayer and Other: games I've played on stream but not finished, and streams that aren't a game,
/// like game dev and IRL. Grouped by the year I last played them, newest first. Worked out from the VOD titles on
/// every build, so finishing a game moves it to Finished by itself.
/// </summary>
public static class PlayedPage
{
    public const string UnfinishedFile = "unfinished.html";
    public const string MultiplayerFile = "multiplayer.html";
    public const string OtherFile = "other.html";

    public static string FileFor(GameList list) => list switch
    {
        GameList.Multiplayer => MultiplayerFile,
        GameList.Other => OtherFile,
        _ => UnfinishedFile,
    };

    /// <param name="list">Unfinished, Multiplayer or Other.</param>
    /// <param name="games">The games with a page. Only those on <paramref name="list"/> are shown.</param>
    public static string Render(GameList list, IEnumerable<Game> games, DateOnly updated)
    {
        var played = games.Where(g => g.IsOn(list)).Select(g => Shown.Of(g, list))
            .OrderByDescending(s => s.LastPlayed).ThenBy(s => s.Game.Name, StringComparer.OrdinalIgnoreCase).ToList();
        var years = played.Select(s => s.LastPlayed.Year).Distinct().ToList();
        var vods = played.SelectMany(s => s.Vods).DistinctBy(v => v.Id).ToList();
        var hours = TimeSpan.FromTicks(vods.Sum(v => v.Duration.Ticks)).TotalHours;

        var (name, title, lede, description) = list switch
        {
            GameList.Multiplayer => ("Multiplayer games", """Multiplayer <span class="grad-text">games</span>""",
                "Games without an ending, played with friends, chat or strangers. Grouped by the year I last played them.",
                "Multiplayer games Morfah has played on stream, with the VODs."),
            GameList.Other => ("Other streams", """Other <span class="grad-text">streams</span>""",
                "Streams that aren't a game, like game dev and IRL. Grouped by the year I last streamed them.",
                "Morfah's streams that aren't a game, like game dev and IRL, with the VODs."),
            _ => ("Unfinished games", """Unfinished <span class="grad-text">games</span>""",
                "Games I've played on stream but haven't finished, at least not yet. Grouped by the year I last played them.",
                "Games Morfah has played on stream but not finished yet, with the VODs."),
        };
        // What a card is, for the counts: a game, or a Twitch category like IRL.
        var (one, many) = list == GameList.Other ? ("category", "categories") : ("game", "games");

        var body = new StringBuilder($"""
                <section class="hero wrap">
                  <p class="eyebrow">Morfah on Twitch</p>
                  <h1>{title}</h1>
                  <p class="lede">{lede}</p>
                  <dl class="stats">
                    <div><dt>{many}</dt><dd>{played.Count}</dd></div>
                    <div><dt>VODs</dt><dd>{vods.Count}</dd></div>
                    <div><dt>hours</dt><dd>{Math.Round(hours).ToString(CultureInfo.InvariantCulture)}</dd></div>
                  </dl>
                </section>


            """);
        // The co-op filter, when some of the games are co-op.
        var chips = played.Any(s => s.Coop.Count > 0) ? """

                      <button class="chip" type="button" data-filter="all" aria-pressed="true">All</button>
                      <button class="chip" type="button" data-filter="coop" aria-pressed="false">Co-op</button>
            """ : "";
        body.Append(GameCards.Toolbar(years, chips, one));
        body.Append("""
                <div class="wrap">

            """);
        foreach (var year in years)
        {
            var cards = played.Where(s => s.LastPlayed.Year == year).Select(s => Card(s, list)).ToList();
            body.Append(GameCards.Year(year, cards, one, many));
        }
        body.Append($"""
                </div>
            {GameCards.Empty(one)}
            """);

        var page = new PageInfo(FileFor(list), FileFor(list), $"{name} · Morfah", description, updated) { Script = "site.js" };
        return Layout.Page(page, body.ToString());
    }

    /// <summary>"Finished" only on Multiplayer, for a game like Splitgate. On Unfinished, the card is the playthrough I haven't finished.</summary>
    private static string Card(Shown shown, GameList list)
    {
        var coop = shown.Coop.Select(p => p.Name).ToList();
        return GameCards.Card(shown.Game.Name, shown.Game, coop.Count > 0 ? GameCards.CoopBadge : "",
            [GameCards.VodsNote(shown.Vods, shown.Missing), list == GameList.Multiplayer && shown.Game.IsFinished ? "Finished" : null, GameCards.CoopNote(coop)],
            coop.Count > 0 ? " data-coop" : "");
    }

    /// <summary>
    /// What a list shows of a game. Unfinished shows only the playthrough I haven't finished, so a finished game, like
    /// Valheim, counts its co-op VODs there and goes by when I last played them. The other lists show all of it.
    /// </summary>
    private sealed record Shown(Game Game, IReadOnlyList<Vod> Vods, IReadOnlyList<MissingVod> Missing, IReadOnlyList<CoopPartner> Coop)
    {
        public static Shown Of(Game game, GameList list) => list == GameList.Unfinished && game.Unfinished is { } unfinished
            ? new(game, game.VodsOf(unfinished), game.MissingVodsOf(unfinished), unfinished.Coop)
            : new(game, game.Vods, game.MissingVods, game.Coop);

        /// <summary>A game on a list has a VOD, or a missing one.</summary>
        public DateOnly LastPlayed => Vods.Select(v => v.Date).Concat(Missing.Select(m => m.Date)).Max();
    }
}
