# Morfah's stream site

The games I've finished on stream, each with its own page of VODs, and every VOD from my YouTube VOD channel: https://morfah.github.io/stream/

A C# program builds the pages from the files in `data`, my VODs on YouTube and game covers from Twitch. GitHub Actions publishes them to GitHub Pages on every push and once a day, so new VODs show up by themselves.

## Data

Edit these by hand, on GitHub or on my PC. A mistake in a file stops the build and the Actions run says where, so the live site stays as it was.

- `data/finished-games.json`: the games I've finished, newest first. One line per game:
  ```json
  { "year": 2025, "name": "Hollow Knight: Silksong" },
  { "year": 2024, "name": "Resident Evil 6", "coop": "franticalle" },
  { "year": 2022, "name": "God of War Ragnarök", "hundredPercent": true, "difficulty": "Hardest" },
  ```
- `data/games.json`: only for games that need help finding their cover or their VODs.
  - `twitch`: the Twitch category's name, when it isn't the game's name.
  - `twitchId`: the Twitch category's ID, when several share a name, like an old game and its remake.
  - `cover`: my own picture in the repo, like `data/covers/game.jpg`, when Twitch has none.
  - `aliases`: other names my VOD titles use for the game, like `["Silksong"]` for Hollow Knight: Silksong.
- `data/vod-fixes.json`: VODs whose titles don't name their games, or name the wrong ones. A VOD's ID and the games it really has, which replaces what its title gives. `[]` means no game:
  ```json
  {
    // Lego Builders Journey and Milk game - 2023-01-06
    "5_HA8oaV_Nk": ["LEGO Builder's Journey", "Milk outside a bag of milk outside a bag of milk"]
  }
  ```
- `data/missing-vods.json`: streams whose VOD is lost, like ones not exported from Twitch in time. Their games' pages show a 😢 card in the VOD list. `title` is optional:
  ```json
  { "date": "2025-09-21", "games": ["Hollow Knight: Silksong"], "title": "Silksong - Part 23" },
  ```

Each build lists what it couldn't do by itself, in the console and on the Actions run's page: games without a cover, and VOD titles without a usable date. To find a game's Twitch category and ID:

```
dotnet run --project src/MorfStudios.StreamSite -- search "Tomb Raider"
```

Several categories can share a name, like an old game and its remake. Check the cover on the site, and use `twitchId` to pick the right one.

## VODs

Every public video on [@MorfahVODs](https://www.youtube.com/@MorfahVODs) is in the archive. The stream's date comes from the title, like `Silksong - Part 43 - 2025-10-14`, otherwise from the upload day. The site shows the title without the date.

A VOD is on a game's page when the game's name, or one of its `aliases`, is in the title. The words must be in order, but case and punctuation don't matter. Where names overlap, the longest wins, so `The Evil Within 2` isn't also The Evil Within. Tags don't count, since many are left over from earlier VODs.

[review.html](https://morfah.github.io/stream/review.html), which isn't in the menu, lists what may need a fix: finished games without VODs, VODs with several games and VODs without a game, each with its ID. Fix them in `data/vod-fixes.json`, with an alias, or by renaming the VOD on YouTube. To list VODs with their IDs:

```
dotnet run --project src/MorfStudios.StreamSite -- vods silksong
```

## Building on my PC

```
dotnet run --project src/MorfStudios.StreamSite
dotnet run serve.cs
```

The first command builds the site into `_site`. The second shows it at http://localhost:5317/stream/.

The build needs the Twitch keys for covers and the YouTube key for VODs. GitHub Actions gets them from the repo secrets. On my PC, put them in `keys.local.json` in the repo folder, which git ignores:

```json
{
  "TWITCH_CLIENT_ID": "...",
  "TWITCH_CLIENT_SECRET": "...",
  "YOUTUBE_API_KEY": "..."
}
```

Without them, the build still works, but games show their name instead of a cover, and the VOD archive is empty.
