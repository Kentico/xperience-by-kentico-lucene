(() => {
    "use strict";

    const filters = document.querySelector(".search-filters");
    if (!filters) {
        return;
    }

    const mobile = window.matchMedia("(max-width: 800px)");
    const updateFilters = () => {
        filters.open = !mobile.matches;
    };

    updateFilters();
    mobile.addEventListener("change", updateFilters);
})();

