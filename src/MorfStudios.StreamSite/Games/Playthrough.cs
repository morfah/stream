using MorfStudios.StreamSite.Data;

namespace MorfStudios.StreamSite.Games;

/// <summary>
/// One time through a game: a line in the finished list, or the one I haven't finished. A game played more than once,
/// like Valheim solo and then co-op, has its VODs split by playthrough on its page. They're split by day, from
/// <see cref="From"/> until the next one's.
/// </summary>
public sealed class Playthrough
{
    /// <summary>The finished list's line, or null for the playthrough I haven't finished.</summary>
    public FinishedGame? Finish { get; init; }

    /// <summary>Who I played it with. Empty when I played alone.</summary>
    public required IReadOnlyList<CoopPartner> Coop { get; init; }

    /// <summary>The first day its VODs can have. The first playthrough has every VOD before the second.</summary>
    public DateOnly From { get; init; } = DateOnly.MinValue;

    /// <summary>The day the next playthrough starts.</summary>
    public DateOnly Until { get; init; } = DateOnly.MaxValue;

    public bool Has(DateOnly day) => day >= From && day < Until;

    /// <summary>
    /// Oldest first. Each starts on its <c>started</c> day, otherwise the day after the one before it finished, so a VOD
    /// after the credits, like Valheim's exploring, stays with its playthrough until the next one starts.
    /// </summary>
    /// <param name="finishes">The game's lines in the finished list.</param>
    /// <param name="info">
    /// Its entry in <c>data/games.json</c>, whose <c>started</c> and <c>coop</c> are for the playthrough I haven't finished.
    /// A finished game with <c>coop</c> there has <c>started</c> too, or the files don't load.
    /// </param>
    /// <param name="partners">Who is who in <c>coop</c>, keyed with <see cref="Names.Key"/>.</param>
    public static IReadOnlyList<Playthrough> Of(IReadOnlyList<FinishedGame> finishes, GameInfo? info, IReadOnlyDictionary<string, CoopPartner> partners)
    {
        var starts = new List<(DateOnly Start, FinishedGame? Finish, IReadOnlyList<string> Coop)>();
        DateOnly? previous = null;
        foreach (var finish in finishes.OrderBy(f => f.Date))
        {
            starts.Add((finish.Started ?? previous?.AddDays(1) ?? DateOnly.MinValue, finish, finish.Coop));
            previous = finish.Date;
        }
        // A game I haven't finished is one playthrough. A finished one has another when data/games.json says when it started.
        if (finishes.Count == 0 || info?.Started is not null)
            starts.Add((info?.Started ?? DateOnly.MinValue, null, info?.Coop ?? []));

        var sorted = starts.OrderBy(s => s.Start).ToList();
        return sorted.Select((s, i) => new Playthrough
        {
            Finish = s.Finish,
            // The names are checked and written as data/coop-partners.json does when the files load.
            Coop = s.Coop.Select(name => partners[Names.Key(name)]).ToList(),
            From = i == 0 ? DateOnly.MinValue : s.Start,
            Until = i + 1 < sorted.Count ? sorted[i + 1].Start : DateOnly.MaxValue,
        }).ToList();
    }
}
