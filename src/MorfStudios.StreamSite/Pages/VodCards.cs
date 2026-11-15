using MorfStudios.StreamSite.Games;
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
    /// <param name="milestones">Badges on the thumbnail, from <see cref="Milestones"/>.</param>
    public static string Card(Vod vod, string milestones = "")
    {
        var play = vod.Embeddable ? $""" data-video="{E(vod.Id)}" data-title="{E(vod.ShortTitle)}" data-date="{E(vod.LongDate)}" """.TrimEnd() : "";
        return $"""
                      <li class="card" data-name="{E(Names.Key(vod.Title))}">
                        <a class="vod" href="{E(vod.WatchUrl)}"{play}>
                          <span class="thumb"><img src="{E(vod.ThumbnailUrl)}" alt="" loading="lazy" width="480" height="360">{milestones}<span class="duration">{vod.Length}</span><span class="play"></span></span>
                          <span class="vod-title">{E(vod.ShortTitle)}</span>
                          <span class="vod-date">{E(vod.LongDate)}</span>
                        </a>
                      </li>

            """;
    }

    /// <summary>
    /// "🏁 Finished" and "🏆 100%" badges for the VOD where I got the credits or 100%, naming the games in their tooltips.
    /// Empty when nothing happened in the VOD.
    /// </summary>
    public static string Milestones(IEnumerable<(Game Game, Milestone Kind)> milestones)
    {
        var list = milestones.ToList();
        var finished = list.Where(m => m.Kind.HasFlag(Milestone.Finished)).Select(m => m.Game.Name).ToList();
        var hundred = list.Where(m => m.Kind.HasFlag(Milestone.HundredPercent)).Select(m => m.Game.Name).ToList();
        if (finished.Count == 0 && hundred.Count == 0)
            return "";

        var badges = (finished.Count > 0 ? $"""<span class="badge" title="{E("Finished " + And(finished))}">🏁 Finished</span>""" : "")
            + (hundred.Count > 0 ? $"""<span class="badge gold" title="{E("100% in " + And(hundred))}">🏆 100%</span>""" : "");
        return $"""<span class="milestones">{badges}</span>""";

        static string And(List<string> names) => names.Count == 1 ? names[0] : string.Join(", ", names[..^1]) + " and " + names[^1];
    }

    /// <summary>A lost VOD in a game's list: a sad face instead of a thumbnail, and nothing to play.</summary>
    /// <param name="milestones">Badges on the thumbnail, from <see cref="Milestones"/>.</param>
    public static string MissingCard(Data.MissingVod missing, Game game, string milestones = "")
    {
        var title = missing.Title ?? string.Join(" & ", missing.Games);
        var date = missing.Date.ToString("ddd d MMM yyyy", System.Globalization.CultureInfo.InvariantCulture);
        return $"""
                      <li class="card" data-name="{E(Names.Key(title))}">
                        <div class="vod missing" title="This VOD is lost.">
                          <span class="thumb">{milestones}<span class="sad" aria-hidden="true">😢</span><span class="missing-text">Missing VOD</span></span>
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
