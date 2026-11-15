using System.Globalization;
using System.Text.RegularExpressions;

namespace MorfStudios.StreamSite.Vods;

/// <summary>
/// Reads my VOD titles, whose format changed over the years: <c>Silksong - Part 43 - 2025-10-14</c>,
/// <c>L.A. Noire | 2024-02-29</c>, <c>Making a 2d game | [VOD] 2024-09-18</c>, <c>Strange Brigade - Part 8 - [CO-OP] (2020-07-08) Archive</c>.
/// </summary>
public static partial class VodTitles
{
    /// <summary>The stream's day, and 1 or 2 for an A or B after it (two streams that day), otherwise 0.</summary>
    public static (DateOnly Date, int Order)? ParseDate(string title)
    {
        var match = DateInTitle().Match(title);
        if (!match.Success)
            return null;

        var text = $"{match.Groups["year"]}-{match.Groups["month"]}-{match.Groups["day"]}";
        if (!DateOnly.TryParseExact(text, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
            return null;

        var order = match.Groups["part"].Value switch { "A" => 1, "B" => 2, _ => 0 };
        return (date, order);
    }

    /// <summary>
    /// The title without its date, <c>[VOD]</c> and <c>Archive</c>, and the separators they leave:
    /// <c>Silksong - Part 43 - 2025-10-14</c> becomes <c>Silksong - Part 43</c>, and
    /// <c>Science museum - 2022-10-01 - PART 2/2</c> becomes <c>Science museum - PART 2/2</c>.
    /// </summary>
    public static string Shorten(string title)
    {
        const string Date = "\0";
        var text = DateToken().Replace(title, Date, 1);
        text = VodTag().Replace(text, " ");
        text = TrailingArchive().Replace(text, "");
        text = DateBeforeSeparator().Replace(text, " ");
        text = DateAtEnd().Replace(text, "");
        text = text.Replace(Date, " ");
        text = Spaces().Replace(text, " ").Trim();
        text = TrailingSeparators().Replace(text, "");
        return text.Length > 0 ? text : title;
    }

    /// <summary>2025-10-14, plus the one title written 202207-19.</summary>
    [GeneratedRegex(@"(?<year>\d{4})-?(?<month>\d{2})-(?<day>\d{2})(?:\s+(?<part>[AB])\b)?")]
    private static partial Regex DateInTitle();

    /// <summary>The date with what belongs to it: brackets around it, an A or B, and "Archive" inside the brackets.</summary>
    [GeneratedRegex(@"[\[(]?\s*\d{4}-?\d{2}-\d{2}(?:\s+[AB]\b)?(?:\s+Archive)?\s*[\])]?")]
    private static partial Regex DateToken();

    [GeneratedRegex(@"\[VOD\]", RegexOptions.IgnoreCase)]
    private static partial Regex VodTag();

    [GeneratedRegex(@"\s+Archive\s*$")]
    private static partial Regex TrailingArchive();

    /// <summary>"X - date - Y": the date and the separator before it go, the one after it stays.</summary>
    [GeneratedRegex(@"\s*[-–|]?\s*\0\s*(?=[-–|]\s)")]
    private static partial Regex DateBeforeSeparator();

    [GeneratedRegex(@"\s*[-–|]?\s*\0\s*$")]
    private static partial Regex DateAtEnd();

    [GeneratedRegex(@"\s{2,}")]
    private static partial Regex Spaces();

    [GeneratedRegex(@"[\s\-–|]+$")]
    private static partial Regex TrailingSeparators();
}
