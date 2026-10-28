/*
 * AI scope toggle. Clicking an AI label underlines the text of its field.
 * Clicking elsewhere, clicking the label again or pressing Esc removes it.
 * One field at a time.
 */
(function () {
    "use strict";

    const LABEL_SELECTOR = "button.ai-label[aria-controls]";

    const setPressed = (label, pressed) => {
        label.setAttribute("aria-pressed", String(pressed));
        document.getElementById(label.getAttribute("aria-controls"))?.classList.toggle("is-highlighted", pressed);
    };

    const releaseAll = (except) => {
        document.querySelectorAll(`${LABEL_SELECTOR}[aria-pressed="true"]`).forEach((label) => {
            if (label !== except) {
                setPressed(label, false);
            }
        });
    };

    document.addEventListener("click", (e) => {
        const label = e.target.closest(LABEL_SELECTOR);
        releaseAll(label);
        if (label) {
            setPressed(label, label.getAttribute("aria-pressed") !== "true");
        }
    });

    document.addEventListener("keydown", (e) => {
        if (e.key === "Escape") {
            releaseAll(null);
        }
    });
})();
