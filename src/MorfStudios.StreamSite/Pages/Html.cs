namespace MorfStudios.StreamSite.Pages;

public static class Html
{
    /// <summary>Escapes text for HTML, in element content and in quoted attributes.</summary>
    public static string E(string? text) => text is null ? "" : text
        .Replace("&", "&amp;")
        .Replace("<", "&lt;")
        .Replace(">", "&gt;")
        .Replace("\"", "&quot;")
        .Replace("'", "&#39;");

    public static string Plural(int count, string word) => $"{count} {word}{(count == 1 ? "" : "s")}";

    /// <summary>Like "A", "A and B", or "A, B and C".</summary>
    public static string And(IEnumerable<string> items)
    {
        var list = items.ToArray();
        return list.Length <= 1 ? string.Concat(list) : string.Join(", ", list[..^1]) + " and " + list[^1];
    }
}
