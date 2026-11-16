using MorfStudios.StreamSite.Data;
using MorfStudios.StreamSite.Games;
using MorfStudios.StreamSite.Vods;
using static MorfStudios.StreamSite.Pages.Html;

namespace MorfStudios.StreamSite.Pages;

/// <summary>Cover cards grouped by year, with year links and search, shared by Finished, Unfinished and Multiplayer.</summary>
public static class GameCards
{
    /// <param name="name">The name on the card, which for a finished game is the finished list's name that time.</param>
    /// <param name="game">The game's page, with its cover.</param>
    /// <param name="badges">HTML, like <c>&lt;span class="badge"&gt;Co-op&lt;/span&gt;</c>.</param>
    /// <param name="notes">HTML, joined with dots under the name.</param>
    /// <param name="flags">Attributes for the filters, like <c> data-coop</c>.</param>
    public static string Card(string name, Game game, string badges, IEnumerable<string?> notes, string flags = "")
    {
        var cover = game.Cover?.Small;
        var art = cover is null
            ? $"<span>{E(name)}</span>"
            : $"""<img src="{E(cover)}" alt="" loading="lazy" width="{Twitch.CoverFinder.Width}" height="{Twitch.CoverFinder.Height}">""";
        var badgeList = badges.Length > 0 ? $"""<div class="badges">{badges}</div>""" : "";
        var text = string.Join(" · ", notes.OfType<string>());
        var note = text.Length > 0 ? $"""{"\n"}            <p class="note">{text}</p>""" : "";

        return $"""
                      <li class="card" data-name="{E(Names.Key(name))}"{flags}>
                        <a href="{E(game.Path)}">
                          <div class="cover{(cover is null ? " no-art" : "")}">{art}{badgeList}</div>
                          <h3>{E(name)}</h3>{note}
                        </a>
                      </li>

            """;
    }

    /// <summary>On the cover of a game I played with others.</summary>
    public const string CoopBadge = """<span class="badge">Co-op</span>""";

    /// <summary>"Co-op with Nyakuzaa", or "Co-op with franticalle and Nyakuzaa". Null when I played alone.</summary>
    public static string? CoopNote(IReadOnlyCollection<string> names) => names.Count > 0 ? $"Co-op with {And(names.Select(E))}" : null;

    /// <summary>"3 VODs", or a sad face when they're all lost.</summary>
    public static string VodsNote(Game game) => VodsNote(game.Vods, game.MissingVods);

    /// <summary>The same for some of the game's VODs, like one playthrough's.</summary>
    public static string VodsNote(IReadOnlyCollection<Vod> vods, IReadOnlyCollection<MissingVod> missing) =>
        vods.Count > 0 ? Plural(vods.Count, "VOD") : missing.Count > 0 ? "VOD missing 😢" : "No VODs";

    /// <summary>The bar under the hero: links to the years, then the filters and search.</summary>
    /// <param name="chips">Filter buttons before the search, as HTML lines. Empty for none.</param>
    /// <param name="one">What a card is, like "game" or "category", for the search box.</param>
    public static string Toolbar(IEnumerable<int> years, string chips = "", string one = "game") => $"""
            <div class="toolbar">
              <div class="wrap">
                <nav class="years" aria-label="Years">{string.Concat(years.Select(y => $"""<a href="#y{y}">{y}</a>"""))}</nav>
                <div class="filters">{chips}
                  <input class="search" id="search" type="search" placeholder="Find a {one}" aria-label="Find a {one}">
                </div>
              </div>
            </div>

        """;

    /// <summary>A year's heading and its cards.</summary>
    /// <param name="one">What a card is, for the count, like "game" or "category". The search counts with it too.</param>
    /// <param name="many">The same for several.</param>
    public static string Year(int year, IReadOnlyCollection<string> cards, string one = "game", string many = "games") => $"""
            <section class="year" id="y{year}">
              <div class="year-head"><h2 class="grad-text">{year}</h2><span class="count" data-one="{one}" data-many="{many}">{cards.Count} {(cards.Count == 1 ? one : many)}</span></div>
              <ol class="grid">

        """ + string.Concat(cards) + """
              </ol>
            </section>

        """;

    /// <summary>Shown by the search when no card matches.</summary>
    /// <param name="one">What a card is, like "game" or "category".</param>
    public static string Empty(string one = "game") => $"""
            <p class="empty wrap" id="empty" hidden>No {one} matches that.</p>
        """;
}
