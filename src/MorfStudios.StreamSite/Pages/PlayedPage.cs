using System.Globalization;
using System.Text;
using MorfStudios.StreamSite.Games;

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
        var played = games.Where(g => g.IsOn(list)).OrderByDescending(g => g.LastPlayed).ThenBy(g => g.Name, StringComparer.OrdinalIgnoreCase).ToList();
        var years = played.Select(g => g.LastPlayed!.Value.Year).Distinct().ToList();
        var vods = played.SelectMany(g => g.Vods).DistinctBy(v => v.Id).ToList();
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
        body.Append(GameCards.Toolbar(years, one: one));
        body.Append("""
                <div class="wrap">

            """);
        foreach (var year in years)
        {
            var cards = played.Where(g => g.LastPlayed!.Value.Year == year)
                .Select(g => GameCards.Card(g.Name, g, "", [GameCards.VodsNote(g), g.IsFinished ? "Finished" : null])).ToList();
            body.Append(GameCards.Year(year, cards, one, many));
        }
        body.Append($"""
                </div>
            {GameCards.Empty(one)}
            """);

        var page = new PageInfo(FileFor(list), FileFor(list), $"{name} · Morfah", description, updated) { Script = "site.js" };
        return Layout.Page(page, body.ToString());
    }
}
