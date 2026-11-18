# Morfah's stream site

The games I've finished on stream, the ones I haven't finished yet, multiplayer games and other streams like game dev and IRL, each with its own page of VODs, and every VOD from my YouTube VOD channel: https://morfah.github.io/stream/

A C# program builds the pages from the files in `data`, my VODs on YouTube and game covers from Twitch. GitHub Actions publishes them to GitHub Pages on every push and once a day, so new VODs show up by themselves.

## Data

Edit these by hand, on GitHub or on my PC. A mistake in a file stops the build and the Actions run says where, so the live site stays as it was.

- `data/finished-games.json`: the games I've finished. `date` is the day I got the credits, and `hundredPercent` the day I got every achievement or trophy, if I did. `coop` is who I played it with: one name, or a list for a party, like `["franticalle", "Nyakuzaa"]`. One line per playthrough, so a game finished twice with different friends has two lines, `started` can say when the second one started, and `playthrough` can name it (see [Playthroughs](#playthroughs)). The site sorts them by date, newest first, so a new line can go anywhere in the file:
  ```json
  { "date": "2025-10-14", "name": "Hollow Knight: Silksong" },
  { "date": "2024-01-25", "name": "Resident Evil 6", "coop": "franticalle" },
  { "date": "2022-12-03", "name": "God of War Ragnarök", "hundredPercent": "2023-01-28", "difficulty": "Hardest" },
  ```
  On the game's page and in the VOD archive, its last VOD on each of those days gets a 🏁 Finished or 🏆 100% badge. The dates before November 2026 were guessed from the VOD titles (FINALE, THE END, Credits, 100%, Platinum), otherwise the game's last VOD that year, and then checked by hand.
- `data/games.json`: only for games that need help finding their cover or their VODs, that are multiplayer, co-op, a DLC, a mod or hidden, or that I've rated. A game here that isn't finished is on Unfinished or Multiplayer once a VOD title names it.
  - `twitch`: the Twitch category's name, when it isn't the game's name.
  - `twitchId`: the Twitch category's ID, when several share a name, like an old game and its remake.
  - `cover`: my own picture in the repo, like `data/covers/game.jpg`, when Twitch has none.
  - `aliases`: other names my VOD titles use for the game, like `["Silksong"]` for Hollow Knight: Silksong.
  - `multiplayer`: `true` for a game without an ending, like Apex Legends. It's on Multiplayer instead of Unfinished. One I'm done with, like Splitgate, goes in the finished list too, and is on both Finished and Multiplayer.
  - `coop`: who I played the playthrough I haven't finished with, like in the finished list: `"Nyakuzaa"` for DEVOUR, or everyone from every stream, like `["franticalle", "Nyakuzaa", "Atahree", "Drast0n"]` for Phasmophobia. For a finished game, that's a playthrough after the finish, like Valheim co-op after my solo one, so it needs `started`. The finished playthroughs' partners go on their lines in the finished list.
  - `started`: for a finished game, the day a playthrough I haven't finished started, like `"2022-02-16"` for Valheim co-op. It adds that playthrough to the game.
  - `rating`: my rating, 1 to 5 stars, like `4`. The stars are on the game's cover on Finished, Unfinished and Multiplayer, and on its page as "My rating". The ratings came from my [Grouvee](https://www.grouvee.com/) export in November 2026, by name and by the version my VODs show, like Tomb Raider 1 and the Resident Evil 2 remake. A game I rate later gets its `rating` by hand. The export, `data/grouvee_export.json`, has my account's details, so git ignores it.
  - `dlc`: `true` for an add-on to another game, like Horizon Zero Dawn's The Frozen Wilds. `mod`: `true` for a game changed by fans, like Silent Hill 2: Enhanced Edition. Either one, not both. It's a DLC or Mod badge on the game's cover and page, and "dlc" or "mod" in the search finds it.
  - `hidden`: `true` for a game I don't want listed. It's not on Unfinished or Multiplayer and has no page, but its VODs stay in the archive.
  - `category`: `true` for a kind of stream that isn't a game, like `Software and Game Development` or `IRL`. It has a page like a game, listed on Other instead of Unfinished. Its VODs come from `data/vod-fixes.json` or other names.
- `data/coop-partners.json`: everyone I play co-op with. `coop` in the other files names them as `name` does here, so a misspelled name stops the build. `twitch` is their Twitch name, when they have a channel, and the site links to it:
  ```json
  { "name": "Nyakuzaa", "twitch": "Nyakuzaa" },
  { "name": "Atahree" },
  ```
  Co-op games get a Co-op badge and a "Co-op with …" note on Finished, Unfinished and Multiplayer, where the Co-op filter finds them. The game's page lists everyone I played it with.
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

## Playthroughs

Each line in the finished list is a playthrough, and so is a game I haven't finished, or a finished one I'm playing again with `started` in `data/games.json`. A game with more than one has its VODs split by playthrough on its page, each with who I played with and how it ended:

```json
// data/finished-games.json
{ "date": "2021-04-05", "name": "Valheim" },
// data/games.json
{ "name": "Valheim", "started": "2022-02-16", "coop": "franticalle" },
```

Valheim's page then has the solo playthrough's VODs, then the co-op one's. It's on Finished for the solo one and on Unfinished for the co-op one, whose card counts only its VODs. With `"multiplayer": true`, it's on Multiplayer instead of Unfinished.

The VODs are split by day: a playthrough has them from its start until the next one starts, so VODs after the credits, like Valheim's exploring, stay with it. A finished playthrough starts on its `started` day, otherwise the day after the one before it finished, like We Were Here Forever's second one.

A collection I finish one game at a time, like Mass Effect Legendary Edition, has a line for each game, with its name in `playthrough`. That's the playthrough's heading on the game's page instead of "Solo playthrough", and it's on the game's card on Finished, where the search finds it:

```json
// data/finished-games.json
{ "date": "2023-02-16", "name": "Mass Effect Legendary Edition", "playthrough": "Mass Effect 2" },
{ "date": "2022-01-28", "name": "Mass Effect Legendary Edition", "playthrough": "Mass Effect" },
// data/games.json
{ "name": "Mass Effect Legendary Edition", "aliases": ["Mass Effect", "Mass Effect 2"] },
```

The `aliases` put VODs like `Mass Effect 2 - 2023-02-09` on the collection's page.

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
