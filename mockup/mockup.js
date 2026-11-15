// Mockup only. The real site gets its pages from the C# build, and keeps just the
// filter and player parts of this script.

// Covers: Twitch's old name-based box art links. They only work for older games, so the
// rest show the no-art card here. The real build gets every cover from Twitch's API by game ID.
const noCover = new Set([
  "Night Delivery", "DreadOut 2", "Vampire Survivors", "Milk inside a bag of milk inside a bag of milk", "Bramble: The Mountain King", "The Evil Within: The Consequence", "Split Fiction", "If On A Winter's Night, Four Travelers", "Okaeri", "Stray", "Deltarune Chapter 1&2", "We Were Here Forever", "The Evil Within: The Assignment", "Fluffy Cubed", "Stigmatized Property", "Cult of the Lamb", "Scorn", "Figment 2: Creed Valley", "Amnesia: The Bunker", "DreadOut", "Burning Daylight", "Inunaki Tunnel", "Five Nights at Freddy's: Security Breach", "Milk outside a bag of milk outside a bag of milk", "Cultic", "The Evil Within: The Executioner", "God of War Ragnarök", "Downfall", "The Ghost Train", "Nightmare of Decay", "Astro's Playground", "Metal: Hellsinger", "The Convenience Store", "Onryo", "Missing Children 行方不明", "G String", "Sanctum", "100ft Robot Golf", "The Night Way Home | 帰り道", "The Radio Station | 深夜放送", "The Closing Shift | 閉店事件", "We Were Here Expeditions: The FriendShip", "Freddy Fazbear’s Pizzeria Simulator", "LEGO Builder’s Journey", "BEAR, VODKA, STALINGRAD! 🐻",
  "Palworld", "Inbento", "Ultimate Custom Night", "The Bathhouse", "JOLLY", "Battlefield 6", "Overwatch 2", "PaRappa the Rapper", "The Caregiver", "Hanako", "Yuki Onna", "Aka Manto", "Christmas Massacre", "Xonotic", "Nexuiz",
]);
const coverName = { "Horizon Zero Dawn - The Frozen Wilds DLC": "Horizon Zero Dawn", "Sonic Mania - Encore DLC": "Sonic Mania", "ELDEN RING": "Elden Ring" };

const coverUrl = (name, width, height) => noCover.has(name)
  ? null
  : `https://static-cdn.jtvnw.net/ttv-boxart/${encodeURIComponent(coverName[name] ?? name)}-${width}x${height}.jpg`;

const escape = text => String(text).replace(/[&<>"']/g, c => ({ "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;" })[c]);
const plural = (count, word) => `${count} ${word}${count === 1 ? "" : "s"}`;
const norm = text => text.toLowerCase().normalize("NFKD").replace(/[̀-ͯ]/g, "").replace(/[’']/g, "").replace(/[^a-z0-9]+/g, " ").trim();
const gameLink = name => `game.html?game=${encodeURIComponent(name)}`;

// From reading all 1012 VOD titles on the channel: VODs per game, matched by the game's name
// or a short name in the title. The real build does this with the YouTube API on every run.
const finishedVodCounts = new Map(Object.entries({
  "Sable": 4, "Split Fiction": 4, "Hollow Knight: Silksong": 41, "Metal: Hellsinger": 6, "Resident Evil 6": 10, "The Evil Within 2": 9, "We Were Here Expeditions: The FriendShip": 1, "We Were Here Forever": 8, "Max Payne 3": 2, "Max Payne": 5, "The Evil Within": 11, "DreadOut 2": 3, "DreadOut": 3, "Super Seducer": 5, "Resident Evil 5": 5, "Amnesia: The Bunker": 2, "Diablo IV": 13, "Figment 2: Creed Valley": 2, "ELDEN RING": 69, "Bramble: The Mountain King": 2, "Horizon Zero Dawn - The Frozen Wilds DLC": 3, "Mass Effect 2": 14, "Yakuza 0": 26, "LEGO Builder’s Journey": 1, "The Radio Station | 深夜放送": 2, "The Night Way Home | 帰り道": 1, "God of War Ragnarök": 27, "Astro's Playground": 1, "God of War": 5, "In Sound Mind": 7, "Cultic": 4, "Scorn": 3, "Nightmare of Decay": 3, "The Closing Shift | 閉店事件": 1, "Cult of the Lamb": 10, "The Mortuary Assistant": 5, "Tomb Raider": 15, "Stray": 1, "The Gardens Between": 2, "A Short Hike": 1, "Figment": 3, "Fluffy Cubed": 1, "Silent Hill": 12, "Ibb & Obb": 6, "Vampire Survivors": 2, "Resident Evil 4": 9, "A Hat in Time": 5, "Subnautica: Below Zero": 13, "Five Nights at Freddy's: Security Breach": 7, "Donut County": 1, "The Room: Old Sins": 3, "Splitgate": 13, "Sayonara Wild Hearts": 1, "Slime Rancher": 7, "Gorogoa": 1, "BEAR, VODKA, STALINGRAD! 🐻": 1, "Superliminal": 1, "If On A Winter's Night, Four Travelers": 2,"Old Man's Journey": 1, "Resident Evil 3": 3, "Deltarune Chapter 1&2": 7, "Resident Evil 2": 8, "Until Dawn": 3, "Life Is Strange 2": 11, "Subnautica": 26, "Night Delivery": 1, "Onryo": 1, "The Convenience Store": 1, "Inunaki Tunnel": 1, "Stigmatized Property": 1, "Okaeri": 1, "Little Nightmares II": 2, "Little Misfortune": 1, "Fran Bow": 4, "Spyro Reignited Trilogy": 15, "Freddy Fazbear’s Pizzeria Simulator": 2, "Ori and the Will of the Wisps": 6, "Lorelai": 2, "Crash Bandicoot N. Sane Trilogy": 5, "Detention": 1, "No Man's Sky": 14, "Downfall": 3, "The Cat Lady": 3, "Valheim": 34, "It Takes Two": 5, "Fall Guys: Ultimate Knockout": 10, "Horace": 6, "Celeste": 6, "Minecraft": 12, "Terraria": 27, "100ft Robot Golf": 2, "Roundabout": 2, "Freedom Planet": 3, "G String": 5, "The Beginner's Guide": 1, "Don't Starve Together": 8, "Cyberpunk 2077": 23, "Sonic Mania": 4, "Visage": 6, "Five Nights at Freddy's: Sister Location": 2, "Five Nights at Freddy's 4: The Final Chapter": 4, "Five Nights at Freddy's 3": 6, "Five Nights at Freddy's 2": 2, "Five Nights at Freddy's": 18, "Amnesia: Rebirth": 3, "Battlefield V": 3, "Monster Boy and The Cursed Kingdom": 7, "Wonder Boy: The Dragon's Trap": 2, "Unravel Two": 2, "We Were Here Together": 6, "Unravel": 2, "StarCraft II: Legacy of the Void": 6, "Doom Eternal": 15, "We Were Here Too": 2, "We Were Here": 2, "Kill to Collect": 6, "Doom 64": 2, "Sanctum": 4, "Risk of Rain 2": 9, "Dusk": 3, "Warhammer: Vermintide 2": 8, "Star Wars Battlefront II": 3, "Strange Brigade": 13, "Wolfenstein: Youngblood": 11,
}).map(([name, count]) => [norm(name), count]));
const vodCount = name => finishedVodCounts.get(norm(name)) ?? 0;

// Games without an end, played with or against others. The real site gets this from a list I keep.
const multiplayerNames = new Set([
  "Apex Legends", "Valorant", "Overwatch 2", "League of Legends", "Counter-Strike: Global Offensive", "Halo Infinite",
  "Battlefield 2042", "Battlefield 6", "Xonotic", "Nexuiz", "Gartic Phone", "Steep", "Phasmophobia", "GTFO",
]);

// Games in VOD titles that aren't on the finished list: name, VODs, year last played.
const playedGames = [
  ["Stardew Valley", 33, 2022], ["The Legend of Zelda: Breath of the Wild", 25, 2023], ["Quake", 38, 2023], ["L.A. Noire", 15, 2025],
  ["Mass Effect", 8, 2022], ["Apex Legends", 8, 2021], ["Palworld", 5, 2024], ["Portal 2", 4, 2021], ["GTFO", 3, 2024], ["Inbento", 3, 2022],
  ["Snakebird", 3, 2021], ["Halo Infinite", 4, 2021], ["A Way Out", 3, 2021], ["Ultimate Custom Night", 3, 2021], ["inFamous: Second Son", 3, 2020],
  ["Dark Souls", 3, 2018], ["Phasmophobia", 3, 2022], ["The Bathhouse", 2, 2022], ["Battlefield 2042", 4, 2021], ["Little Nightmares", 2, 2021],
  ["JOLLY", 2, 2021], ["GRIS", 2, 2021], ["Battlefield 6", 1, 2025], ["Grand Theft Auto", 2, 2025], ["System Shock", 2, 2023],
  ["Final Fantasy XVI", 1, 2023], ["Overwatch 2", 1, 2022], ["The Wonderful 101", 1, 2022], ["Klonoa", 1, 2022], ["PaRappa the Rapper", 1, 2022],
  ["Valorant", 3, 2022], ["League of Legends", 1, 2022], ["Steep", 1, 2021], ["Gartic Phone", 1, 2021], ["DEVOUR", 2, 2021],
  ["The Caregiver", 1, 2021], ["Hanako", 1, 2021], ["Counter-Strike: Global Offensive", 1, 2021], ["Yuki Onna", 1, 2021], ["Aka Manto", 1, 2021],
  ["The Red Solstice", 1, 2021], ["The Stanley Parable", 1, 2021], ["Christmas Massacre", 1, 2021], ["Xonotic", 1, 2011], ["Nexuiz", 1, 2011], ["Dap", 1, 2022],
].map(([name, vods, lastPlayed]) => ({ name, vods, lastPlayed, multiplayer: multiplayerNames.has(name) }));
const unfinishedGames = playedGames.filter(g => !g.multiplayer);
const multiplayerGames = playedGames.filter(g => g.multiplayer);

// ---------- Cards and year sections, shared by both game lists ----------

function card(name, { badges = "", notes = [], flags = [] } = {}) {
  const url = coverUrl(name, 285, 380);
  const art = url
    ? `<img src="${url}" alt="" loading="lazy" width="285" height="380">`
    : `<span>${escape(name)}</span>`;
  const note = notes.filter(Boolean).join(" · ");
  return `<li class="card" data-name="${escape(norm(name))}"${flags.map(f => ` data-${f}`).join("")}>
    <a href="${gameLink(name)}">
      <div class="cover${url ? "" : " no-art"}">${art}${badges ? `<div class="badges">${badges}</div>` : ""}</div>
      <h3>${escape(name)}</h3>
      ${note ? `<p class="note">${note}</p>` : ""}
    </a>
  </li>`;
}

const finishedBadges = game => [
  game.hundredPercent ? `<span class="badge gold">🏆 100%</span>` : "",
  game.coop ? `<span class="badge">Co-op</span>` : "",
  game.difficulty ? `<span class="badge" title="${escape(game.difficulty)} difficulty">💀</span>` : "",
].join("");

const vodsNote = count => count ? plural(count, "VOD") : "No VODs found";

function showYears(groups) {
  document.getElementById("years").innerHTML = groups.map(g => `<a href="#y${g.year}">${g.year}</a>`).join("");
  document.getElementById("years-list").innerHTML = groups.map(g => `<section class="year" id="y${g.year}">
      <div class="year-head"><h2 class="grad-text">${g.year}</h2><span class="count">${plural(g.cards.length, "game")}</span></div>
      <ol class="grid">${g.cards.join("")}</ol>
    </section>`).join("");
}

function showStats(stats) {
  document.getElementById("stats").innerHTML = stats
    .map(([value, label]) => `<div><dt>${label}</dt><dd>${value}</dd></div>`).join("");
}

function setUpFilters() {
  const chips = [...document.querySelectorAll(".chip")];
  const search = document.getElementById("search");
  let filter = "all";

  function apply() {
    const words = norm(search.value).split(" ").filter(Boolean);
    let shown = 0;
    for (const section of document.querySelectorAll(".year")) {
      let inSection = 0;
      for (const item of section.querySelectorAll(".card")) {
        const visible = (filter === "all" || item.hasAttribute(`data-${filter}`))
          && words.every(w => item.dataset.name.includes(w));
        item.hidden = !visible;
        if (visible) inSection++;
      }
      section.hidden = inSection === 0;
      section.querySelector(".count").textContent = plural(inSection, "game");
      shown += inSection;
    }
    document.getElementById("empty").hidden = shown > 0;
  }

  for (const chip of chips) {
    chip.addEventListener("click", () => {
      filter = chip.dataset.filter;
      for (const c of chips) c.setAttribute("aria-pressed", String(c === chip));
      apply();
    });
  }
  search.addEventListener("input", apply);
}

const loadFinished = async () => (await fetch("../data/finished-games.json")).json();

// ---------- Finished games ----------

async function showFinishedGames() {
  const games = await loadFinished();
  const years = [...new Set(games.map(g => g.year))].sort((a, b) => b - a);
  showStats([
    [games.length, "games finished"],
    [games.filter(g => g.hundredPercent).length, "at 100% 🏆"],
    [years.length, "years of streams"],
  ]);
  showYears(years.map(year => ({
    year,
    cards: games.filter(g => g.year === year).map(g => card(g.name, {
      badges: finishedBadges(g),
      notes: [vodsNote(vodCount(g.name)), g.coop && `Co-op with ${escape(g.coop)}`, g.difficulty && `${escape(g.difficulty)} difficulty`],
      flags: [g.hundredPercent && "hundred", g.coop && "coop"].filter(Boolean),
    })),
  })));
  setUpFilters();
}

// ---------- Unfinished and multiplayer games ----------

function showPlayedGames(games) {
  const years = [...new Set(games.map(g => g.lastPlayed))].sort((a, b) => b - a);
  showStats([
    [games.length, "games"],
    [games.reduce((total, g) => total + g.vods, 0), "VODs"],
  ]);
  showYears(years.map(year => ({
    year,
    // The real site sorts by the date I last played, which the mockup doesn't have.
    cards: games.filter(g => g.lastPlayed === year).sort((a, b) => b.vods - a.vods)
      .map(g => card(g.name, { notes: [vodsNote(g.vods)] })),
  })));
  setUpFilters();
}

// ---------- Game page ----------

// Real VODs from the channel. The mockup only has Silksong's, the real build has every game's.
const silksongVods = [
  ["7I-BLZ-w3so", "Silksong - Part 1 - 2025-09-04", "3:45:51"], ["i8K7yJxwmcc", "Silksong - Part 2 - 2025-09-05", "3:51:50"],
  ["kztpb0qIg7M", "Silksong - Part 3 - 2025-09-06 A", "3:48:41"], ["VjrSMVCzmvQ", "Silksong - Part 4 - 2025-09-06 B", "2:46:51"],
  ["_Vfmq7DJw3w", "Silksong - Part 5 - 2025-09-07 A", "3:05:31"], ["iAktmavaQFI", "Silksong - Part 6 - 2025-09-07 B", "2:02:41"],
  ["nFTw9UPRkl8", "Silksong - Part 7 - 2025-09-08", "2:42:50"], ["blk6F1DR1Zc", "Silksong - Part 8 - 2025-09-09", "2:42:50"],
  ["0v9bI6OXtak", "Silksong - Part 9 - 2025-09-10", "2:57:51"], ["9g8Hj2EKugk", "Silksong - Part 10 - 2025-09-11", "3:10:11"],
  ["XspzdsWho0s", "Silksong - Part 11 - 2025-09-12", "3:05:51"], ["4G4otWXC-_Q", "Silksong - Part 12 - 2025-09-13 A", "2:56:11"],
  ["8e3JIG9N6NM", "Silksong - Part 13 - 2025-09-13 B", "1:59:01"], ["JJM8sgfUWk8", "Silksong - Part 14 - 2025-09-14 A", "3:04:11"],
  ["bifEkC1EPJg", "Silksong - Part 15 - 2025-09-14 B", "2:27:21"], ["L4zy97BnjCk", "Silksong - Part 16 - 2025-09-15", "3:14:21"],
  ["47w2lJNliP4", "Silksong - Part 17 - 2025-09-16", "1:41:51"], ["NJ4MPOxZhRY", "Silksong - Part 18 - 2025-09-17", "3:08:57"],
  ["PzCYDZU69Ew", "Silksong - Part 19 - 2025-09-18", "3:01:51"], ["tdSqbO8PD3c", "Silksong - Part 20 - 2025-09-19", "3:33:01"],
  ["4i277g1SA6E", "Silksong - Part 21 - 2025-09-20 A", "2:26:41"], ["ohDSAqXmxOY", "Silksong - Part 22 - 2025-09-20 B", "3:09:01"],
  ["Lf3cV1uOfe0", "Silksong - Part 25 - 2025-09-22", "2:07:21"], ["YNCILCJHhnc", "Silksong - Part 26 - 2025-09-23", "2:00:21"],
  ["dwIB9R_f3pY", "Silksong - Part 27 - 2025-09-24", "2:20:21"], ["Z1EUZR_cjHY", "Silksong - Part 28 - 2025-09-25", "2:22:01"],
  ["SFzs_p9fNYo", "Silksong - Part 29 - 2025-09-26", "2:03:51"], ["fOVnVtuL0Pw", "Silksong - Part 30 - 2025-09-27 A", "1:46:41"],
  ["4GUTsZQGloA", "Silksong - Part 31 - 2025-09-27 B", "3:00:51"], ["pe4IQKTlPgM", "Silksong - Part 32 - 2025-09-28 A", "2:15:51"],
  ["vUInkIfZkY0", "Silksong - Part 33 - 2025-09-28 B", "2:39:31"], ["8sqy59ZAJmk", "Silksong - Part 34 - 2025-09-29", "2:08:21"],
  ["vE9bFGOvv68", "Silksong - Part 35 - 2025-10-06", "3:26:11"], ["u5rWWxubrTk", "Silksong - Part 36 - 2025-10-07", "2:18:01"],
  ["6q5LOuZJi7A", "Silksong - Part 37 - 2025-10-08", "2:40:21"], ["h8h7eDi9Yos", "Silksong - Part 38 - 2025-10-09", "2:03:31"],
  ["m4VirggoT88", "Silksong - Part 39 - 2025-10-10", "2:15:41"], ["slpyevkKy2Y", "Silksong - Part 40 - 2025-10-11", "2:48:51"],
  ["k5KZVTN3FZE", "Silksong - Part 41 - 2025-10-12", "2:34:51"], ["KJanqP1-Z_Y", "Silksong - Part 42 - 2025-10-13", "2:44:31"],
  ["Y4Az-TRy6IM", "Silksong - Part 43 - 2025-10-14", "1:49:31"],
].map(([id, title, duration]) => ({
  id,
  title: title.replace(/^Silksong - /, "").replace(/\s*[-|]\s*\d{4}-\d{2}-\d{2}( [AB])?$/, ""),
  date: new Date(title.match(/\d{4}-\d{2}-\d{2}/)[0] + "T12:00:00"),
  duration,
}));

const longDate = new Intl.DateTimeFormat("en-GB", { weekday: "short", day: "numeric", month: "short", year: "numeric" });
const shortDate = new Intl.DateTimeFormat("en-GB", { day: "numeric", month: "short" });
const seconds = duration => duration.split(":").reduce((total, part) => total * 60 + Number(part), 0);

async function showGame() {
  const name = new URLSearchParams(location.search).get("game") ?? "Hollow Knight: Silksong";
  const finishes = (await loadFinished()).filter(g => norm(g.name) === norm(name));
  const played = playedGames.find(g => norm(g.name) === norm(name));
  const isFinished = finishes.length > 0;
  const count = isFinished ? vodCount(name) : played?.vods ?? 0;
  const home = isFinished
    ? { href: "./", label: "finished games" }
    : played?.multiplayer
      ? { href: "multiplayer.html", label: "multiplayer games" }
      : { href: "unfinished.html", label: "unfinished games" };

  document.title = `${name} · Morfah's VODs`;
  document.getElementById("game-name").textContent = name;

  const back = document.getElementById("back");
  back.href = home.href;
  back.textContent = `← All ${home.label}`;
  document.querySelector(`.nav a[href="${home.href}"]`)?.setAttribute("aria-current", "page");

  const years = finishes.map(g => g.year).sort();
  document.getElementById("game-status").textContent = isFinished
    ? `Finished on stream in ${years.join(" and ")}`
    : played?.multiplayer
      ? `Multiplayer · last played on stream in ${played.lastPlayed}`
      : `Last played on stream in ${played?.lastPlayed ?? "?"} · not finished yet`;

  const merged = {
    hundredPercent: finishes.some(g => g.hundredPercent),
    coop: finishes.find(g => g.coop)?.coop,
    difficulty: finishes.find(g => g.difficulty)?.difficulty,
  };
  const badges = [
    merged.hundredPercent ? `<span class="badge gold">🏆 100%</span>` : "",
    merged.coop ? `<a class="badge" href="https://www.twitch.tv/${encodeURIComponent(merged.coop)}">Co-op with ${escape(merged.coop)}</a>` : "",
    merged.difficulty ? `<span class="badge">💀 ${escape(merged.difficulty)} difficulty</span>` : "",
  ].join("");
  document.getElementById("game-badges").innerHTML = badges;

  const cover = document.getElementById("game-cover");
  const art = coverUrl(name, 480, 640);
  cover.classList.toggle("no-art", !art);
  cover.innerHTML = art ? `<img src="${art}" alt="${escape(name)} cover" width="480" height="640">` : `<span>${escape(name)}</span>`;
  document.querySelector(".backdrop").style.setProperty("--art", art ? `url("${coverUrl(name, 300, 400)}")` : "none");

  const isSilksong = norm(name) === norm("Hollow Knight: Silksong");
  if (!isSilksong) {
    document.getElementById("facts").innerHTML = `<li>${count ? `<strong>${count}</strong> ${count === 1 ? "VOD" : "VODs"}` : "No VODs found"}</li>`;
    document.getElementById("play-first").hidden = true;
    document.getElementById("vods").outerHTML = `<p class="placeholder">${count
      ? `The mockup only has Silksong's VODs. On the real site, this game's ${plural(count, "VOD")} are listed here.`
      : `No VOD title names this game, so none were found. On the real site I can add VODs to a game by hand.`}</p>`;
    return;
  }

  const vods = silksongVods;
  const hours = Math.round(vods.reduce((total, v) => total + seconds(v.duration), 0) / 3600);
  document.getElementById("facts").innerHTML = [
    `<li><strong>${vods.length}</strong> VODs</li>`,
    `<li><strong>${hours}</strong> hours</li>`,
    `<li>${shortDate.format(vods[0].date)} – ${longDate.format(vods.at(-1).date).replace(/^\w+, /, "")}</li>`,
  ].join("");

  const list = document.getElementById("vods");
  list.innerHTML = vods.map((v, i) => `<li>
    <button class="vod" type="button" data-index="${i}">
      <span class="thumb">
        <img src="https://i.ytimg.com/vi/${v.id}/hqdefault.jpg" alt="" loading="lazy">
        <span class="duration">${v.duration}</span>
        <span class="play"></span>
      </span>
      <span class="vod-title">${escape(v.title)}</span>
      <span class="vod-date">${longDate.format(v.date)}</span>
    </button>
  </li>`).join("");

  const player = document.getElementById("player");
  const frame = player.querySelector("iframe");
  const next = document.getElementById("next");
  let current = -1;

  function play(index) {
    current = index;
    const vod = vods[index];
    frame.src = `https://www.youtube-nocookie.com/embed/${vod.id}?autoplay=1&rel=0`;
    document.getElementById("now-title").textContent = `${vod.title} · ${longDate.format(vod.date)}`;
    document.getElementById("on-youtube").href = `https://www.youtube.com/watch?v=${vod.id}`;
    next.hidden = index === vods.length - 1;
    player.hidden = false;
    for (const button of list.querySelectorAll(".vod"))
      button.setAttribute("aria-current", String(Number(button.dataset.index) === index));
    player.scrollIntoView({ block: "start" });
  }

  list.addEventListener("click", e => {
    const button = e.target.closest(".vod");
    if (button) play(Number(button.dataset.index));
  });
  document.getElementById("play-first").addEventListener("click", () => play(0));
  next.addEventListener("click", () => play(current + 1));
}

if (document.getElementById("years-list")) {
  const page = document.body.dataset.page;
  if (page === "unfinished") showPlayedGames(unfinishedGames);
  else if (page === "multiplayer") showPlayedGames(multiplayerGames);
  else showFinishedGames();
}
if (document.getElementById("vods")) showGame();
