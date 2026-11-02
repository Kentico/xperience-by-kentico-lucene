/*
 * Promo/discount code field — collapsed until the customer asks for it.
 * The form is rendered expanded so it stays usable without JavaScript; this script
 * collapses it on init. The toggle opts in with [data-promotion-toggle]; the collapsed
 * state lives in a single class so the rest of the container's classes are left alone.
 * Focus moves into the panel on open and back to the toggle on Escape.
 *
 * A background cart update replaces the markup this script is bound to, so it binds again
 * on the cart:updated event qtyStepper.js dispatches once the new markup is in place.
 */
(function () {
    "use strict";

    const COLLAPSED_CLASS = "cart-promotion-code--collapsed";

    function initPromotionCode() {
        document.querySelectorAll("[data-promotion-toggle]").forEach((toggle) => {
            if (toggle.dataset.promotionToggleInitialized) {
                return;
            }
            toggle.dataset.promotionToggleInitialized = "true";

            const container = toggle.closest(".cart-promotion-code");
            if (!container) {
                return;
            }

            const panel = document.getElementById(toggle.getAttribute("aria-controls"));

            const setExpanded = (expanded) => {
                container.classList.toggle(COLLAPSED_CLASS, !expanded);
                toggle.setAttribute("aria-expanded", String(expanded));
            };

            // A failed code attempt renders the panel open so the message and the value stay visible.
            setExpanded(container.dataset.promotionOpen === "true");

            toggle.addEventListener("click", () => {
                const expanded = !container.classList.contains(COLLAPSED_CLASS);

                setExpanded(!expanded);

                if (!expanded) {
                    panel?.querySelector("input, button")?.focus();
                }
            });

            panel?.addEventListener("keydown", (event) => {
                if (event.key !== "Escape" || container.classList.contains(COLLAPSED_CLASS)) {
                    return;
                }
                setExpanded(false);
                toggle.focus();
            });
        });
    }

    document.addEventListener("cart:updated", initPromotionCode);

    initPromotionCode();
})();
