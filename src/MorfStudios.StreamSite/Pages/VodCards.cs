using MorfStudios.StreamSite.Vods;
using static MorfStudios.StreamSite.Pages.Html;

namespace MorfStudios.StreamSite.Pages;

/// <summary>VOD cards and the player they open, shared by the VOD archive and the game pages.</summary>
public static class VodCards
{
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

    /// <summary>A lost VOD in a game's list: a sad face instead of a thumbnail, and nothing to play.</summary>
    public static string MissingCard(Data.MissingVod missing, Games.Game game)
    {
        var title = missing.Title ?? string.Join(" & ", missing.Games);
        var date = missing.Date.ToString("ddd d MMM yyyy", System.Globalization.CultureInfo.InvariantCulture);
        return $"""
                      <li class="card" data-name="{E(Names.Key(title))}">
                        <div class="vod missing" title="This VOD is lost.">
                          <span class="thumb"><span class="sad" aria-hidden="true">😢</span><span class="missing-text">Missing VOD</span></span>
                          <span class="vod-title">{E(title)}</span>
                          <span class="vod-date">{E(date)}</span>
                        </div>
                      </li>

            """;
    }

    /// <summary>
    /// The player that VOD cards open. The YouTube player only loads when a VOD is picked.
    /// In a list with <c>data-sequence</c>, like a game's VODs, "Next part" plays the next one.
    /// </summary>
    public const string PlayerDialog = """
          <dialog class="player-dialog" id="player" aria-labelledby="player-title">
            <div class="frame"><iframe title="VOD player" allow="autoplay; encrypted-media; picture-in-picture; fullscreen" allowfullscreen></iframe></div>
            <div class="now">
              <h2 id="player-title"></h2>
              <button class="btn primary" type="button" id="player-next" hidden>Next part →</button>
              <a class="btn" id="player-youtube" href="#" target="_blank" rel="noopener">Open on YouTube</a>
              <button class="btn" type="button" id="player-close">Close</button>
            </div>
          </dialog>
        """;
}
