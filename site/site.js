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
function setUpPlayer() {
  const dialog = document.getElementById("player");
  if (!dialog) return;

  const frame = dialog.querySelector("iframe");

  document.addEventListener("click", event => {
    const link = event.target.closest("a.vod[data-video]");
    if (!link || event.button !== 0 || event.ctrlKey || event.metaKey || event.shiftKey || event.altKey) return;
    event.preventDefault();
    frame.src = `https://www.youtube-nocookie.com/embed/${encodeURIComponent(link.dataset.video)}?autoplay=1&rel=0`;
    document.getElementById("player-title").textContent = `${link.dataset.title} · ${link.dataset.date}`;
    document.getElementById("player-youtube").href = link.href;
    dialog.showModal();
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
