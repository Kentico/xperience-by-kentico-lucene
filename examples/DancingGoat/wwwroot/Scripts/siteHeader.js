/*
 * Site header behavior: scrolled state, mobile menu toggle, dropdown menus
 * (language + user). Progressive enhancement — the header renders fine
 * without it (desktop nav and links work; dropdowns and hamburger need JS).
 */
(() => {
    "use strict";

    const shell = document.querySelector(".nav-shell");
    if (!shell) {
        return;
    }

    const onScroll = () => {
        shell.classList.toggle("scrolled", window.scrollY > 8);
    };
    window.addEventListener("scroll", onScroll, { passive: true });
    onScroll();

    const toggle = shell.querySelector(".nav-toggle");
    const menu = toggle ? document.getElementById(toggle.getAttribute("aria-controls")) : null;

    const isMenuOpen = () => {
        return shell.getAttribute("data-menu-open") === "true";
    };

    const focusFirst = (panel) => {
        const first = panel ? panel.querySelector("a, button, input") : null;
        if (first) {
            first.focus();
        }
    };

    const setMenu = (open, restoreFocus) => {
        if (!toggle) {
            return;
        }

        shell.setAttribute("data-menu-open", open ? "true" : "false");
        toggle.setAttribute("aria-expanded", open ? "true" : "false");

        if (open) {
            focusFirst(menu);
        } else if (restoreFocus) {
            toggle.focus();
        }
    };

    if (toggle) {
        toggle.addEventListener("click", () => {
            setMenu(!isMenuOpen());
        });
        shell.querySelectorAll(".nav-links a").forEach((link) => {
            link.addEventListener("click", () => { setMenu(false); });
        });
    }

    const dropdowns = Array.from(shell.querySelectorAll("[data-open]"));

    const setDropdown = (drop, open, restoreFocus) => {
        const btn = drop.querySelector("button");
        if (!btn) {
            return;
        }

        drop.setAttribute("data-open", open ? "true" : "false");
        btn.setAttribute("aria-expanded", open ? "true" : "false");

        if (open) {
            focusFirst(document.getElementById(btn.getAttribute("aria-controls")));
        } else if (restoreFocus) {
            btn.focus();
        }
    };

    const closeAll = (except, restoreFocus) => {
        dropdowns.forEach((drop) => {
            if (drop !== except) {
                const wasOpen = drop.getAttribute("data-open") === "true";
                setDropdown(drop, false, restoreFocus && wasOpen);
            }
        });
    };

    dropdowns.forEach((drop) => {
        const btn = drop.querySelector("button");
        if (!btn) {
            return;
        }
        btn.addEventListener("click", (e) => {
            e.stopPropagation();
            const open = drop.getAttribute("data-open") !== "true";
            closeAll(drop);
            setDropdown(drop, open);
        });
    });

    document.addEventListener("click", (e) => {
        if (isMenuOpen() && !shell.contains(e.target)) {
            setMenu(false);
        }
        closeAll(null);
    });
    document.addEventListener("keydown", (e) => {
        if (e.key !== "Escape") {
            return;
        }

        const menuWasOpen = isMenuOpen();
        setMenu(false, menuWasOpen);
        closeAll(null, true);
    });
})();
