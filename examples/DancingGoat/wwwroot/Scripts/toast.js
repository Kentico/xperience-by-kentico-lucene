/*
 * Cart toast: shows one of the pills the layout keeps in a <template> and hides it after a
 * moment. Both pills are rendered by the page, so their text and the cart link are always in
 * the page's language.
 *
 * A pill is asked for in one of two ways: the layout marks the slot after a plain form post that
 * redirected, or a background add-to-cart raises the "cart:toast" event.
 *
 * The pill holds the only "View cart" link, so the auto-hide must not take it away while the
 * visitor is reading or reaching for it (WCAG 2.2.1 Timing Adjustable): hovering or focusing
 * anything inside it cancels the timer for good, and a close button is always available.
 *
 * The cookie bar is fixed to the same corner of the viewport and is taller before consent is
 * given, where it would hide the pill completely. Its height is published as
 * --consent-bar-height so the pill can sit above it rather than behind it.
 */
(() => {
    "use strict";

    const HIDE_DELAY = 4000;
    // Slightly longer than the 0.3s fade of .dg-toast, so the fade normally ends first.
    const REMOVE_FALLBACK_DELAY = 400;
    const TOAST_EVENT = "cart:toast";

    const TEMPLATES = {
        added: "[data-cart-toast-added]",
        error: "[data-cart-toast-error]"
    };

    const consentBar = document.getElementById("consent");

    const publishConsentBarHeight = () => {
        document.documentElement.style.setProperty("--consent-bar-height", (consentBar?.offsetHeight ?? 0) + "px");
    };

    window.addEventListener("resize", publishConsentBarHeight);

    const slot = document.querySelector("[data-cart-toast]");

    function show(kind) {
        const selector = TEMPLATES[kind];
        if (!slot || !selector) {
            return;
        }

        const toast = document.querySelector(selector)?.content.cloneNode(true).firstElementChild;
        if (!toast) {
            return;
        }

        // The slot holds one pill at a time, so a second toast replaces the first.
        slot.replaceChildren(toast);

        let timer;
        let removeTimer;

        /*
         * A hidden pill is only transparent, so leaving it in the slot would keep its "View cart"
         * link and close button in the tab order and inside the live region. It is dropped once the
         * fade has finished - unless a newer pill has taken the slot in the meantime. A detached pill
         * or a background tab never reports the end of the fade, so a timer drops it as well.
         */
        const remove = () => {
            window.clearTimeout(removeTimer);
            toast.removeEventListener("transitionend", onFadeEnd);

            if (slot.firstElementChild === toast) {
                slot.replaceChildren();
            }
        };

        // Transitions of anything inside the pill bubble up here and must not end its fade early.
        const onFadeEnd = (event) => {
            if (event.target === toast) {
                remove();
            }
        };

        const hide = () => {
            /*
             * Closing with the keyboard leaves focus on the close button, which is about to be
             * dropped and would send focus back to the top of the page. The cart link in the header
             * takes it instead, as it leads to the same place as the pill's "View cart" link.
             */
            if (toast.contains(document.activeElement)) {
                document.querySelector(".cart-badge")?.closest("a")?.focus();
            }

            toast.classList.remove("show");
            toast.addEventListener("transitionend", onFadeEnd);
            removeTimer = window.setTimeout(remove, REMOVE_FALLBACK_DELAY);
        };

        const cancelHide = () => {
            window.clearTimeout(timer);
        };

        toast.addEventListener("mouseenter", cancelHide);
        toast.addEventListener("focusin", cancelHide);

        toast.querySelector("[data-toast-close]")?.addEventListener("click", () => {
            cancelHide();
            hide();
        });

        requestAnimationFrame(() => {
            publishConsentBarHeight();
            toast.classList.add("show");
        });

        timer = window.setTimeout(hide, HIDE_DELAY);
    }

    document.addEventListener(TOAST_EVENT, (event) => show(event.detail?.kind));

    show(slot?.dataset.cartToastAutoshow);
})();
