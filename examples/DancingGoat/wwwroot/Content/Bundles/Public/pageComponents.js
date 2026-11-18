/*
 * Events slider — translateX paging over .event-track with prev/next arrows
 * and generated dots. Supports multiple slider instances per page; content
 * stays readable without JS (first card visible).
 *
 * Bundled into Content/Bundles/Public/pageComponents.min.js by
 * `npm run build:bundles` and emitted by <page-builder-scripts />, so it also
 * loads inside the Page Builder editor. initEventSliders is exposed so the
 * widget view can initialize instances the editor inserts after load.
 */
(function () {
    "use strict";

    const FOCUSABLE = "a[href], button, input, select, textarea, [tabindex]";
    const supportsInert = "inert" in HTMLElement.prototype;

    const setCardHidden = (card, hidden) => {
        if (supportsInert) {
            card.inert = hidden;
        }
        card.setAttribute("aria-hidden", hidden ? "true" : "false");
        card.querySelectorAll(FOCUSABLE).forEach((element) => {
            if (hidden) {
                if (!element.hasAttribute("data-prev-tabindex")) {
                    element.setAttribute("data-prev-tabindex", element.getAttribute("tabindex") ?? "");
                }
                element.setAttribute("tabindex", "-1");
            } else {
                const previous = element.getAttribute("data-prev-tabindex");
                if (previous === null) {
                    return;
                }
                if (previous === "") {
                    element.removeAttribute("tabindex");
                } else {
                    element.setAttribute("tabindex", previous);
                }
                element.removeAttribute("data-prev-tabindex");
            }
        });
    };

    const initSlider = (slider) => {
        if (slider.dataset.sliderInitialized) {
            return;
        }
        slider.dataset.sliderInitialized = "true";

        const track = slider.querySelector("[data-event-track]");
        if (!track) {
            return;
        }
        const cards = Array.from(track.children);
        if (cards.length < 2) {
            return;
        }

        const prev = slider.querySelector("[data-event-prev]");
        const next = slider.querySelector("[data-event-next]");
        const dotsHost = slider.querySelector("[data-event-dots]");
        const status = slider.querySelector("[data-event-status]");
        let index = 0;
        let announce = false;
        const dots = [];

        if (dotsHost) {
            const dotLabel = dotsHost.dataset.eventDotLabel || "Event {0}";

            cards.forEach((_, i) => {
                const dot = document.createElement("button");
                dot.type = "button";
                dot.className = "event-dot";
                dot.setAttribute("aria-label", dotLabel.replace("{0}", i + 1));
                dot.addEventListener("click", () => goTo(i));
                dotsHost.appendChild(dot);
                dots.push(dot);
            });
        }

        const update = () => {
            track.style.transform = "translateX(-" + (index * 100) + "%)";
            if (prev) { prev.disabled = index === 0; }
            if (next) { next.disabled = index === cards.length - 1; }
            cards.forEach((card, i) => setCardHidden(card, i !== index));
            dots.forEach((dot, i) => {
                if (i === index) {
                    dot.setAttribute("aria-current", "true");
                } else {
                    dot.removeAttribute("aria-current");
                }
            });

            if (status && announce) {
                const template = status.dataset.eventStatusLabel || "";
                status.textContent = template.replace("{0}", index + 1).replace("{1}", cards.length);
            }
            announce = true;
        };

        const goTo = (i) => {
            index = Math.max(0, Math.min(cards.length - 1, i));
            update();
        };

        if (prev) { prev.addEventListener("click", () => goTo(index - 1)); }
        if (next) { next.addEventListener("click", () => goTo(index + 1)); }

        update();
    };

    const initEventSliders = () => document.querySelectorAll("[data-event-slider]").forEach(initSlider);

    window.DancingGoat = window.DancingGoat || {};
    window.DancingGoat.initEventSliders = initEventSliders;

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", initEventSliders);
    } else {
        initEventSliders();
    }
})();
