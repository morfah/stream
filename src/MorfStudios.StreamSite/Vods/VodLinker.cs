using MorfStudios.StreamSite.Data;
using MorfStudios.StreamSite.Games;

namespace MorfStudios.StreamSite.Vods;

/// <summary>Which games each VOD has: from <c>data/vod-fixes.json</c> if it's there, otherwise from <see cref="VodMatcher"/>.</summary>
public static class VodLinker
{
    /// <summary>Gives each game its VODs, oldest first.</summary>
    public static VodLinks Link(IReadOnlyList<Vod> vods, IReadOnlyList<Game> games, IReadOnlyDictionary<string, IReadOnlyList<string>> fixes, Report report)
    {
        var matcher = new VodMatcher(games);
        var unknown = new List<string>();
        var fixedGames = fixes.ToDictionary(fix => fix.Key, fix => (IReadOnlyList<Game>)fix.Value
            .Select(name =>
            {
                var game = matcher.Find(name);
                if (game is null)
                    unknown.Add($"{name} ({fix.Key})");
                return game;
            })
            .OfType<Game>()
            .Distinct()
            .ToList());
        // A typo, or a game I haven't finished, which only finished games have a page for yet.
        report.Add("Games in data/vod-fixes.json that aren't on the site, so they're left out. Check the spelling:", unknown);

        var gamesByVod = vods.ToDictionary(vod => vod.Id, vod => fixedGames.GetValueOrDefault(vod.Id) ?? matcher.Match(vod));

        foreach (var game in games)
            game.Vods = Vod.OldestFirst(vods.Where(v => gamesByVod[v.Id].Contains(game))).ToList();

        var onChannel = vods.Select(v => v.Id).ToHashSet();
        report.Add("data/vod-fixes.json has VODs that aren't on the channel, maybe deleted or private now:",
            fixes.Keys.Where(id => !onChannel.Contains(id)));

        return new VodLinks(gamesByVod, fixedGames.Keys.Where(onChannel.Contains).ToHashSet());
    }

    /// <summary>Gives each game its missing VODs, from <c>data/missing-vods.json</c>, oldest first.</summary>
    public static void AddMissing(IReadOnlyList<Game> games, IReadOnlyList<MissingVod> missing, Report report)
    {
        var matcher = new VodMatcher(games);
        var unknown = new List<string>();
        var byGame = new Dictionary<Game, List<MissingVod>>();
        foreach (var vod in missing)
        {
            foreach (var name in vod.Games)
            {
                if (matcher.Find(name) is not { } game)
                {
                    unknown.Add($"{name} ({vod.Date:yyyy-MM-dd})");
                    continue;
                }
                if (!byGame.TryGetValue(game, out var list))
                    byGame[game] = list = [];
                list.Add(vod);
            }
        }
        foreach (var (game, list) in byGame)
            game.MissingVods = list.OrderBy(m => m.Date).ToList();

        report.Add("Games in data/missing-vods.json that aren't on the site, so they're left out. Check the spelling:", unknown);
    }
}

/// <param name="GamesByVod">Each VOD's games, by VOD ID. Empty for VODs without a game.</param>
/// <param name="Fixed">VODs whose games come from data/vod-fixes.json.</param>
public sealed record VodLinks(IReadOnlyDictionary<string, IReadOnlyList<Game>> GamesByVod, IReadOnlySet<string> Fixed);
