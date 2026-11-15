# Morfah's stream site

The games I've finished on stream, with their covers: https://morfah.github.io/stream/

A C# program builds the pages from the files in `data`, and GitHub Actions publishes them to GitHub Pages on every push and once a day.

## Data

Edit these by hand, on GitHub or on my PC. A mistake in a file stops the build and the Actions run says where, so the live site stays as it was.

- `data/finished-games.json`: the games I've finished, newest first. One line per game:
  ```json
  { "year": 2025, "name": "Hollow Knight: Silksong" },
  { "year": 2024, "name": "Resident Evil 6", "coop": "franticalle" },
  { "year": 2022, "name": "God of War Ragnarök", "hundredPercent": true, "difficulty": "Hardest" },
  ```
- `data/games.json`: only for games that need help finding their cover.
  - `twitch`: the Twitch category's name, when it isn't the game's name.
  - `twitchId`: the Twitch category's ID, when several share a name, like an old game and its remake.
  - `cover`: my own picture in the repo, like `data/covers/game.jpg`, when Twitch has none.

Each build lists the games it found no cover for, in the console and on the Actions run's page. To find a game's Twitch category and ID:

```
dotnet run --project src/MorfStudios.StreamSite -- search "Tomb Raider"
```

Several categories can share a name, like an old game and its remake. Check the cover on the site, and use `twitchId` to pick the right one.

## Building on my PC

```
dotnet run --project src/MorfStudios.StreamSite
dotnet run serve.cs
```

The first command builds the site into `_site`. The second shows it at http://localhost:5317/stream/.

Covers need the Twitch keys. GitHub Actions gets them from the repo secrets. On my PC, put them in `keys.local.json` in the repo folder, which git ignores:

```json
{
  "TWITCH_CLIENT_ID": "...",
  "TWITCH_CLIENT_SECRET": "..."
}
```

Without them, the build still works, but games show their name instead of a cover.
