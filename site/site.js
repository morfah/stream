// Filters and search on the game lists. The pages work without it, just without filtering.

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
        const visible = (filter === "all" || card.hasAttribute(`data-${filter}`))
          && words.every(word => card.dataset.name.includes(word));
        card.hidden = !visible;
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
      for (const other of chips) other.setAttribute("aria-pressed", String(other === chip));
      apply();
    });
  }
  search.addEventListener("input", apply);
  // A search typed before the script ran, or kept by the browser on Back.
  if (search.value) apply();
}

setUpFilters();
