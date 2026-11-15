using System.Text;
using MorfStudios.StreamSite.Games;
using MorfStudios.StreamSite.Vods;
using static MorfStudios.StreamSite.Pages.Html;

namespace MorfStudios.StreamSite.Pages;

/// <summary>
/// For me, not in the menu: where the matching of VODs to games may need a fix. Every VOD shows its ID,
/// ready for <c>data/vod-fixes.json</c>.
/// </summary>
public static class ReviewPage
{
    public const string File = "review.html";

    public static string Render(IReadOnlyList<Game> games, IReadOnlyList<Vod> vods, VodLinks links, DateOnly updated)
    {
        // A game whose only VODs are missing is explained already.
        var withoutVods = games.Where(g => g.Vods.Count == 0 && g.MissingVods.Count == 0).ToList();
        var missing = games.SelectMany(g => g.MissingVods.Select(m => (Missing: m, Game: g))).OrderBy(m => m.Missing.Date).ToList();
        var withoutGame = vods.Where(v => links.GamesByVod[v.Id].Count == 0).ToList();
        var several = vods.Where(v => links.GamesByVod[v.Id].Count > 1).ToList();
        var page = new PageInfo(File, "", "Review · Morfah", "Where the VODs and games may need a fix.", updated) { Hidden = true };

        var body = new StringBuilder($"""
                <section class="hero wrap">
                  <p class="eyebrow">Only for me</p>
                  <h1>Review</h1>
                  <p class="lede">Where a VOD's games may need a fix. To fix a VOD, put its ID in <code>data/vod-fixes.json</code> with the games it really has, or rename it on YouTube so the title names the game.</p>
                  <dl class="stats">
                    <div><dt>VODs with a game</dt><dd>{vods.Count - withoutGame.Count}</dd></div>
                    <div><dt>without a game</dt><dd>{withoutGame.Count}</dd></div>
                    <div><dt>fixed by hand</dt><dd>{links.Fixed.Count}</dd></div>
                  </dl>
                </section>

                <div class="wrap review">

            """);

        Section(body, "Finished games without VODs", "No VOD title names these. Their streams may have titles like \"Short indie games\".",
            withoutVods.Select(g => $"""<li><a href="{E(g.Path)}">{E(g.Name)}</a></li>"""));
        Section(body, "VODs with several games", "Right when I played several games that stream.",
            several.Select(v => Row(v, string.Join(", ", links.GamesByVod[v.Id].Select(g => g.Name)))));
        Section(body, "VODs without a game", "Fine for game dev, events and other streams without a game.",
            withoutGame.Select(v => Row(v, null)));
        Section(body, "Missing VODs", "Lost streams from data/missing-vods.json, shown on their games' pages.",
            missing.Select(m => $"""<li><span class="date">{m.Missing.Date:yyyy-MM-dd}</span> <a href="{E(m.Game.Path)}">{E(m.Game.Name)}</a>{(m.Missing.Title is { } t ? $" · {E(t)}" : "")}</li>"""));

        body.Append("""
                </div>
            """);
        return Layout.Page(page, body.ToString());
    }

    private static void Section(StringBuilder body, string title, string hint, IEnumerable<string> items)
    {
        var list = items.ToList();
        body.Append($"""
                  <section class="review-section">
                    <h2>{E(title)} <span>{list.Count}</span></h2>
                    <p>{E(hint)}</p>

            """);
        if (list.Count == 0)
            body.Append("""      <p class="none">None.</p>""" + "\n");
        else
            body.Append($"""      <ul>{"\n        "}{string.Join("\n        ", list)}{"\n"}      </ul>""" + "\n");
        body.Append("""
                  </section>

            """);
    }

    private static string Row(Vod vod, string? games) =>
        $"""<li><code class="id">{E(vod.Id)}</code> <span class="date">{vod.Date:yyyy-MM-dd}</span> <a href="{E(vod.WatchUrl)}">{E(vod.Title)}</a>{(games is null ? "" : $" <span class=\"games\">→ {E(games)}</span>")}</li>""";
}
