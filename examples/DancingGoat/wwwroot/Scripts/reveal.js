/*
 * Reveal on scroll. Opt-in: hidden state only applies after html.js-reveal is
 * set, so no-JS users (and reduced-motion users, via CSS) see everything.
 * No-ops in the Page Builder interface (edit and read-only alike), where widgets
 * rendered after load are never observed and would sit at opacity 0 forever.
 */
(function () {
    "use strict";

    if (!("IntersectionObserver" in window)) {
        return;
    }
    if (document.documentElement.dataset.pageBuilder === "true") {
        return;
    }

    document.documentElement.classList.add("js-reveal");

    const elements = Array.from(document.querySelectorAll(".reveal"));
    const reveal = (element) => element.classList.add("in");

    const observer = new IntersectionObserver((entries) => {
        entries.forEach((entry) => {
            if (entry.isIntersecting) {
                reveal(entry.target);
                observer.unobserve(entry.target);
            }
        });
    }, { threshold: 0.12 });

    elements.forEach((element) => {
        if (element.getBoundingClientRect().top < window.innerHeight * 0.95) {
            reveal(element);
        } else {
            observer.observe(element);
        }
    });
})();
