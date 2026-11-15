using System.Globalization;
using System.Text;

namespace MorfStudios.StreamSite;

public static class Names
{
    /// <summary>
    /// A game name reduced to lowercase letters and digits with single spaces, for comparing names
    /// written differently: "ELDEN RING" and "Elden Ring", "Ragnarök" and "Ragnarok", "L.A. Noire" and "L.A Noire".
    /// Apostrophes are dropped, so "Don't" matches "Dont". Other characters, like Japanese, are dropped too.
    /// </summary>
    public static string Key(string name)
    {
        var decomposed = name.ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var key = new StringBuilder(decomposed.Length);
        var gap = false;
        foreach (var c in decomposed)
        {
            if (c is '\'' or '’' || CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark)
                continue;
            if (c is >= 'a' and <= 'z' or >= '0' and <= '9')
            {
                if (gap && key.Length > 0)
                    key.Append(' ');
                key.Append(c);
                gap = false;
            }
            else
            {
                gap = true;
            }
        }
        return key.ToString();
    }
}
