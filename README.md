# Morfah's stream site

The games I've finished on stream, the ones I haven't finished yet, multiplayer games, each with its own page of VODs, and every VOD from my YouTube VOD channel: https://morfah.github.io/stream/

A C# program builds the pages from the files in `data`, my VODs on YouTube and game covers from Twitch. GitHub Actions publishes them to GitHub Pages on every push and once a day, so new VODs show up by themselves.

## Data

Edit these by hand, on GitHub or on my PC. A mistake in a file stops the build and the Actions run says where, so the live site stays as it was.

- `data/finished-games.json`: the games I've finished. `date` is the day I got the credits, and `hundredPercent` the day I got every achievement or trophy, if I did. One line per playthrough, so a game finished twice with different friends has two lines. The site sorts them by date, newest first, so a new line can go anywhere in the file:
  ```json
  { "date": "2025-10-14", "name": "Hollow Knight: Silksong" },
  { "date": "2024-01-25", "name": "Resident Evil 6", "coop": "franticalle" },
  { "date": "2022-12-03", "name": "God of War Ragnarök", "hundredPercent": "2023-01-28", "difficulty": "Hardest" },
  ```
  On the game's page and in the VOD archive, its last VOD on each of those days gets a 🏁 Finished or 🏆 100% badge. The dates before November 2026 were guessed from the VOD titles (FINALE, THE END, Credits, 100%, Platinum), otherwise the game's last VOD that year, and then checked by hand.
- `data/games.json`: only for games that need help finding their cover or their VODs, or that are multiplayer or hidden. A game here that isn't finished is on Unfinished or Multiplayer once a VOD title names it.
  - `twitch`: the Twitch category's name, when it isn't the game's name.
  - `twitchId`: the Twitch category's ID, when several share a name, like an old game and its remake.
  - `cover`: my own picture in the repo, like `data/covers/game.jpg`, when Twitch has none.
  - `aliases`: other names my VOD titles use for the game, like `["Silksong"]` for Hollow Knight: Silksong.
  - `multiplayer`: `true` for a game without an ending, like Apex Legends. It's on Multiplayer instead of Unfinished. One I'm done with, like Splitgate, goes in the finished list too, and is on both Finished and Multiplayer.
  - `hidden`: `true` for a game I don't want listed. It's not on Unfinished or Multiplayer and has no page, but its VODs stay in the archive.
- `data/not-games.json`: pieces of VOD titles that have a Twitch category's name but aren't a game I played, like `"Birthday"` in `Birthday 🥳 | Learning Quake speedrunning`.
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

Each build lists what it couldn't do by itself, in the console and on the Actions run's page: games without a cover, VOD titles without a usable date, and names in the data files that no VOD title has. To find a game's Twitch category and ID:

```
dotnet run --project src/MorfStudios.StreamSite -- search "Tomb Raider"
```

Several categories can share a name, like an old game and its remake. Check the cover on the site, and use `twitchId` to pick the right one.

## VODs

Every public video on [@MorfahVODs](https://www.youtube.com/@MorfahVODs) is in the archive. The stream's date comes from the title, like `Silksong - Part 43 - 2025-10-14`, otherwise from the upload day. The site shows the title without the date.

A VOD is on a game's page when the game's name, or one of its `aliases`, is in the title. The words must be in order, but case and punctuation don't matter. Where names overlap, the longest wins, so `The Evil Within 2` isn't also The Evil Within. Tags don't count, since many are left over from earlier VODs.

[review.html](https://morfah.github.io/stream/review.html), which isn't in the menu, lists what may need a fix: finished games without VODs, the games found on Twitch and the title pieces they came from, VODs with several games and VODs without a game, each with its ID. Fix them in `data/vod-fixes.json`, `data/not-games.json`, with an alias, or by renaming the VOD on YouTube. To list VODs with their IDs:

```
dotnet run --project src/MorfStudios.StreamSite -- vods silksong
```

## Unfinished and multiplayer games

Games I've played but not finished aren't stored anywhere: every build finds them in the VOD titles. A title is split into pieces at ` - `, ` | `, commas, `&`, `+` and "and", and a piece with the same name as a Twitch category is a game, so `Palworld - 2024-02-13` is enough for a new game to show up. Case and punctuation don't matter, and neither do part numbers, `[notes]`, `(notes)` or "with Nyakuzaa".

- A demo or beta counts as the game: `Final Fantasy XVI Demo` is Final Fantasy XVI. If I've finished the game, the demo VOD is on its page with the others, like `Diablo IV Beta`.
- Unfinished and Multiplayer are grouped by the year I last played the game, newest first.
- To mark a game finished, add it to `data/finished-games.json`. The next build moves it to Finished.
- A piece that isn't a game I played goes in `data/not-games.json`. The wrong version of a game, or the wrong name, needs an entry in `data/games.json`, like `{ "name": "System Shock", "twitchId": "516082" }` for the remake.
- Without the Twitch keys, only the games in the data files are found.

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
