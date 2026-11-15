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
                body.Append(Card(vod));
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

            {PlayerDialog}
            """);

        return Layout.Page(File, "VOD archive · Morfah", "Every stream on Morfah's YouTube VOD channel, by year.", body.ToString(), updated, "site.js");
    }

    /// <summary>
    /// A link to the VOD on YouTube. With JavaScript, a click plays it in <see cref="PlayerDialog"/> instead,
    /// unless embedding is off for it (no <c>data-video</c>).
    /// </summary>
    public static string Card(Vod vod)
    {
        var play = vod.Embeddable ? $""" data-video="{E(vod.Id)}" data-title="{E(vod.ShortTitle)}" data-date="{E(vod.LongDate)}" """.TrimEnd() : "";
        return $"""
                      <li class="card" data-name="{E(Names.Key(vod.Title))}">
                        <a class="vod" href="{E(vod.WatchUrl)}"{play}>
                          <span class="thumb"><img src="{E(vod.ThumbnailUrl)}" alt="" loading="lazy" width="480" height="360"><span class="duration">{vod.Length}</span><span class="play"></span></span>
                          <span class="vod-title">{E(vod.ShortTitle)}</span>
                          <span class="vod-date">{E(vod.LongDate)}</span>
                        </a>
                      </li>

            """;
    }

    /// <summary>The player that VOD cards open. The YouTube player only loads when a VOD is picked.</summary>
    public const string PlayerDialog = """
          <dialog class="player-dialog" id="player" aria-labelledby="player-title">
            <div class="frame"><iframe title="VOD player" allow="autoplay; encrypted-media; picture-in-picture; fullscreen" allowfullscreen></iframe></div>
            <div class="now">
              <h2 id="player-title"></h2>
              <a class="btn" id="player-youtube" href="#" target="_blank" rel="noopener">Open on YouTube</a>
              <button class="btn" type="button" id="player-close">Close</button>
            </div>
          </dialog>
        """;
}
