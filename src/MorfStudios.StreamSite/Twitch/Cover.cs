namespace MorfStudios.StreamSite.Twitch;

/// <summary>A game's cover in two sizes, as paths from the site's root, like <c>covers/1234.jpg</c>.</summary>
/// <param name="Small">For the game lists, <see cref="CoverFinder.Width"/> wide.</param>
/// <param name="Large">For the game's page and link previews, <see cref="CoverFinder.LargeWidth"/> wide.</param>
public sealed record Cover(string Small, string Large);
