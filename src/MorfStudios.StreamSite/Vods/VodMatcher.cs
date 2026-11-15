using MorfStudios.StreamSite.Data;
using MorfStudios.StreamSite.Games;

namespace MorfStudios.StreamSite.Vods;

/// <summary>
/// Finds the games in a VOD's title: a game's name or other name, ignoring case and punctuation.
/// The longest name wins where names overlap, so "The Evil Within 2" isn't also The Evil Within.
/// Tags don't count: many are left over from an earlier VOD, like "the evil within" on a Max Payne VOD.
/// </summary>
public sealed class VodMatcher
{
    private readonly List<(string Text, Game Game)> _names;
    private readonly Dictionary<string, Game> _byKey = [];

    public VodMatcher(IEnumerable<Game> games)
    {
        foreach (var game in games)
        {
            foreach (var name in game.Aliases.Prepend(game.Name))
            {
                var key = Names.Key(name);
                if (key.Length == 0)
                    continue;
                if (_byKey.TryGetValue(key, out var other) && other != game)
                    throw new DataException($"data/games.json: \"{name}\" is a name of both {other.Name} and {game.Name}.");
                _byKey[key] = game;
            }
        }
        // Spaces around each name, so "doom 64" doesn't match inside "doom 640".
        _names = _byKey.Select(pair => ($" {pair.Key} ", pair.Value)).OrderByDescending(n => n.Item1.Length).ToList();
    }

    /// <summary>The game with this name or other name, ignoring case and punctuation.</summary>
    public Game? Find(string name) => _byKey.GetValueOrDefault(Names.Key(name));

    /// <summary>The games named in the VOD's title.</summary>
    public IReadOnlyList<Game> Match(Vod vod)
    {
        var title = $" {Names.Key(vod.Title)} ";
        var taken = new List<(int Start, int End)>();
        var matches = new List<Game>();

        foreach (var (text, game) in _names)
        {
            for (var start = title.IndexOf(text, StringComparison.Ordinal); start >= 0; start = title.IndexOf(text, start + 1, StringComparison.Ordinal))
            {
                // The spaces at both ends may be shared with a neighbouring name.
                var end = start + text.Length;
                if (taken.Any(t => start + 1 < t.End - 1 && end - 1 > t.Start + 1))
                    continue;
                taken.Add((start, end));
                if (!matches.Contains(game))
                    matches.Add(game);
            }
        }
        return matches;
    }
}
