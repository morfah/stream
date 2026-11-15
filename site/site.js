// Filters, search and the VOD player. The pages work without it: no filtering, and VODs open on YouTube.

// The same as Names.Key in the build: lowercase letters and digits, no accents or apostrophes.
const norm = text => text.toLowerCase().normalize("NFKD").replace(/[̀-ͯ]/g, "").replace(/['’]/g, "").replace(/[^a-z0-9]+/g, " ").trim();
const plural = (count, word) => `${count} ${word}${count === 1 ? "" : "s"}`;

function setUpFilters() {
  const search = document.getElementById("search");
  if (!search) return;

  const chips = [...document.querySelectorAll(".chip")];
  const sections = [...document.querySelectorAll(".year")];
  let filter = "all";

  function apply() {
    const words = norm(search.value).split(" ").filter(Boolean);
    let shown = 0;
    for (const section of sections) {
      let inSection = 0;
      for (const card of section.querySelectorAll(".card")) {
        // Each word must start a word in the name, so "4" finds Part 4 and 43 but not 14.
        const name = " " + card.dataset.name;
        const visible = (filter === "all" || card.hasAttribute(`data-${filter}`))
          && words.every(word => name.includes(" " + word));
        card.hidden = !visible;
        if (visible) inSection++;
      }
      section.hidden = inSection === 0;
      const count = section.querySelector(".count");
      count.textContent = plural(inSection, count.dataset.word ?? "game");
      shown += inSection;
    }
    document.getElementById("empty").hidden = shown > 0;
  }

  for (const chip of chips) {
    chip.addEventListener("click", () => {
      filter = chip.dataset.filter;
      for (const other of chips) other.setAttribute("aria-pressed", String(other === chip));
      apply();
    });
  }
  search.addEventListener("input", apply);
  // A search typed before the script ran, or kept by the browser on Back.
  if (search.value) apply();
}

// VOD links open the player instead of YouTube. The YouTube player only loads then.
// Ctrl, Shift or middle click still opens YouTube, and so do VODs with embedding turned off.
// In a list with data-sequence, like a game's VODs, "Next part" plays the next one.
function setUpPlayer() {
  const dialog = document.getElementById("player");
  if (!dialog) return;

  const frame = dialog.querySelector("iframe");
  const next = document.getElementById("player-next");
  let current = null;

  const nextVod = link => {
    const list = link?.closest("[data-sequence]");
    if (!list) return null;
    const vods = [...list.querySelectorAll("a.vod[data-video]")];
    return vods[vods.indexOf(link) + 1] ?? null;
  };

  function play(link) {
    current?.removeAttribute("aria-current");
    current = link;
    link.setAttribute("aria-current", "true");
    frame.src = `https://www.youtube-nocookie.com/embed/${encodeURIComponent(link.dataset.video)}?autoplay=1&rel=0`;
    document.getElementById("player-title").textContent = `${link.dataset.title} · ${link.dataset.date}`;
    document.getElementById("player-youtube").href = link.href;
    next.hidden = !nextVod(link);
    if (!dialog.open) dialog.showModal();
  }

  const plainClick = event => event.button === 0 && !event.ctrlKey && !event.metaKey && !event.shiftKey && !event.altKey;

  document.addEventListener("click", event => {
    if (!plainClick(event)) return;
    const link = event.target.closest("a.vod[data-video]");
    // "Watch from the start" on a game page plays its first VOD.
    const start = event.target.closest("[data-play-first]") && document.querySelector("[data-sequence] a.vod[data-video]");
    const target = link ?? start;
    if (!target) return;
    event.preventDefault();
    play(target);
  });

  next.addEventListener("click", () => {
    const following = nextVod(current);
    if (following) play(following);
  });
  document.getElementById("player-close").addEventListener("click", () => dialog.close());
  // A click on the dark area around the player closes it too.
  dialog.addEventListener("click", event => {
    if (event.target === dialog) dialog.close();
  });
  // Stops the video. Escape closes the dialog by itself.
  dialog.addEventListener("close", () => frame.src = "about:blank");
}

setUpFilters();
setUpPlayer();
