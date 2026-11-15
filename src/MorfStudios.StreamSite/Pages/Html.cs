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
}
