using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace MorfStudios.StreamSite.Vods;

/// <summary>
/// A piece of a VOD title that may be a game's name, between separators like <c> - </c> and <c> | </c>.
/// <see cref="Parts"/> are tried when <see cref="Whole"/> isn't a game.
/// </summary>
/// <param name="Whole">Like "Papers, Please", or null when it's too short to be a name.</param>
/// <param name="Parts">Split at commas, <c>&amp;</c>, <c>+</c> and "and": "Gaming news" and "Halo Infinite". Empty when there's nothing to split.</param>
public sealed record TitlePiece(string? Whole, IReadOnlyList<string> Parts);

/// <summary>
/// Splits VOD titles into the pieces that may name a game, for <see cref="Games.GameFinder"/>.
/// "Gaming news, Halo Infinite Beta" gives "Gaming news, Halo Infinite Beta" with the parts "Gaming news" and "Halo Infinite".
/// Known games are taken out first, so "BEAR, VODKA, STALINGRAD!" isn't three pieces. A known name followed by a number
/// stays, so "Silent Hill 2" is a piece when only Silent Hill is known.
/// </summary>
public sealed partial class TitlePieces
{
    /// <summary>Stands in for what's taken out of a title, and splits it there.</summary>
    private const string Cut = "\u0001";

    private readonly Regex? _known;

    /// <param name="knownNames">Names and other names of the games the data files know.</param>
    public TitlePieces(IEnumerable<string> knownNames)
    {
        // Longest first, so "Max Payne 3" is taken before "Max Payne". The words of a name may be apart by any punctuation.
        var names = knownNames.Select(Names.Key).Where(k => k.Length > 0).Distinct().OrderByDescending(k => k.Length)
            .Select(k => string.Join("[^A-Za-z0-9]+", k.Split(' ')))
            .ToList();
        if (names.Count > 0)
            _known = new Regex($"(?<![A-Za-z0-9])(?:{string.Join('|', names)})(?![A-Za-z0-9]){NotASequel}", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    }

    /// <summary>Not followed by a number, like "Silent Hill 2" or "Diablo IV".</summary>
    private const string NotASequel = "(?! +(?:[0-9]+|i{1,3}|iv|vi{0,3}|ix|x)(?![A-Za-z0-9]))";

    /// <param name="shortTitle">The title without its date, from <see cref="VodTitles.Shorten"/>.</param>
    public IReadOnlyList<TitlePiece> Find(string shortTitle)
    {
        var text = Brackets().Replace(Simplify(shortTitle), Cut);
        if (_known is not null)
            text = _known.Replace(text, Cut);

        var pieces = new List<TitlePiece>();
        foreach (var piece in Separator().Split(text))
        {
            var whole = Clean(piece);
            var parts = PartSeparator().Split(piece).Select(Clean).OfType<string>().Distinct().ToList();
            if (parts.Count == 1 && parts[0] == whole)
                parts = [];
            if (whole is not null || parts.Count > 0)
                pieces.Add(new TitlePiece(whole, parts));
        }
        return pieces;
    }

    /// <summary>
    /// The piece without part numbers, who I played with, and a demo or beta at the end, which counts as the game.
    /// Null when what's left is too short to be a name, or only digits.
    /// </summary>
    private static string? Clean(string piece)
    {
        var text = PartNumber().Replace(piece, " ");
        text = PlayedWith().Replace(text, "");
        text = DemoOrBeta().Replace(text, "");
        text = Spaces().Replace(text, " ");
        text = TrimEnds(text);
        var key = Names.Key(text).Replace(" ", "");
        return key.Length >= 3 && !key.All(char.IsAsciiDigit) ? text : null;
    }

    /// <summary>Without punctuation, symbols and emoji at the ends: "Portal 2 with Nyakuzaa!" ends at "Nyakuzaa".</summary>
    private static string TrimEnds(string text)
    {
        var start = 0;
        var end = text.Length;
        while (start < end && !char.IsLetterOrDigit(text[start]))
            start++;
        while (end > start && !char.IsLetterOrDigit(text[end - 1]))
            end--;
        return text[start..end];
    }

    /// <summary>Accents and apostrophes gone, like <see cref="Names.Key"/>, but with the case and the separators kept.</summary>
    private static string Simplify(string title)
    {
        var text = new StringBuilder(title.Length);
        foreach (var c in title.Normalize(NormalizationForm.FormD))
        {
            if (c is not ('\'' or '’') && CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                text.Append(c);
        }
        return text.ToString();
    }

    /// <summary>Notes like [COOP], [Hardcore] and (PS1). No game names in there so far.</summary>
    [GeneratedRegex(@"\[[^\]]*\]?|\([^)]*\)?")]
    private static partial Regex Brackets();

    /// <summary>Between pieces: what was taken out, <c> - </c>, <c> | </c> and dashes.</summary>
    [GeneratedRegex("\u0001|[|–—]|(?:^|\\s)-(?:\\s|$)")]
    private static partial Regex Separator();

    /// <summary>Within a piece: commas, <c>&amp;</c>, <c>+</c>, "and", and a full stop after a word ("React. Christmas Massacre").</summary>
    [GeneratedRegex(@",|\s[&+]\s|\sand\s|(?<=[A-Za-z]{2})\.\s", RegexOptions.IgnoreCase)]
    private static partial Regex PartSeparator();

    /// <summary>"Part 4", "pt2", "PART 2/2".</summary>
    [GeneratedRegex(@"\b(?:part|pt)\.?\s*[0-9]+(?:\s*/\s*[0-9]+)?\b", RegexOptions.IgnoreCase)]
    private static partial Regex PartNumber();

    /// <summary>"with Nyakuzaa", "/w Nyakuzaa", "w/ friends", to the end of the piece.</summary>
    [GeneratedRegex(@"(?:^|\s)(?:with|/w|w/)(?:\s.*)?$", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex PlayedWith();

    /// <summary>"Final Fantasy XVI Demo", "Halo Infinite Beta", "Battlefield 2042 early access".</summary>
    [GeneratedRegex(@"(?:^|\s+)(?:demo|beta|early access)\W*$", RegexOptions.IgnoreCase)]
    private static partial Regex DemoOrBeta();

    [GeneratedRegex(@"\s+")]
    private static partial Regex Spaces();
}
