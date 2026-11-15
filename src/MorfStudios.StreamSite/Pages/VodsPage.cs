using System.Globalization;
using System.Text;
using MorfStudios.StreamSite.Vods;
using static MorfStudios.StreamSite.Pages.Html;

namespace MorfStudios.StreamSite.Pages;

/// <summary>The VOD archive: every VOD on my YouTube channel, grouped by year, newest first.</summary>
public static class VodsPage
{
    public const string File = "vods.html";

    /// <param name="vods">Newest first, from <see cref="Vod.NewestFirst"/>.</param>
    public static string Render(IReadOnlyList<Vod> vods, DateOnly updated)
    {
        var years = vods.Select(v => v.Date.Year).Distinct().OrderDescending().ToList();
        var hours = (int)Math.Round(vods.Sum(v => v.Duration.TotalHours));
        var body = new StringBuilder();

        body.Append($"""
                <section class="hero wrap">
                  <p class="eyebrow">Morfah on Twitch</p>
                  <h1>VOD <span class="grad-text">archive</span></h1>
                  <p class="lede">Every stream on my YouTube VOD channel. Pick one to watch it here.</p>
                  <dl class="stats">
                    <div><dt>VODs</dt><dd>{vods.Count.ToString("N0", CultureInfo.InvariantCulture)}</dd></div>
                    <div><dt>hours of streams</dt><dd>{hours.ToString("N0", CultureInfo.InvariantCulture)}</dd></div>
                    <div><dt>first VOD</dt><dd>{(years.Count > 0 ? years[^1] : "-")}</dd></div>
                  </dl>
                </section>

                <div class="toolbar">
                  <div class="wrap">
                    <nav class="years" aria-label="Years">{string.Concat(years.Select(y => $"""<a href="#y{y}">{y}</a>"""))}</nav>
                    <div class="filters">
                      <input class="search" id="search" type="search" placeholder="Find a VOD" aria-label="Find a VOD">
                    </div>
                  </div>
                </div>

                <div class="wrap">

            """);

        foreach (var year in years)
        {
            var inYear = vods.Where(v => v.Date.Year == year).ToList();
            body.Append($"""
                    <section class="year" id="y{year}">
                      <div class="year-head"><h2 class="grad-text">{year}</h2><span class="count" data-word="VOD">{Plural(inYear.Count, "VOD")}</span></div>
                      <ol class="vods">

                """);
            foreach (var vod in inYear)
                body.Append(VodCards.Card(vod));
            body.Append("""
                      </ol>
                    </section>

                """);
        }

        if (vods.Count == 0)
            body.Append("""
                    <p class="empty">No VODs yet.</p>

                """);

        body.Append($"""
                </div>
                <p class="empty wrap" id="empty" hidden>No VOD matches that.</p>

            {VodCards.PlayerDialog}
            """);

        var page = new PageInfo(File, File, "VOD archive · Morfah", "Every stream on Morfah's YouTube VOD channel, by year.", updated) { Script = "site.js" };
        return Layout.Page(page, body.ToString());
    }
}
