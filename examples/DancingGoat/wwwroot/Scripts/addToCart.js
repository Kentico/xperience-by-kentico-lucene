/*
 * Add to cart — posts the add form in the background, so the store and product detail pages
 * neither reload nor scroll and the visitor keeps their place in the product list.
 *
 * Forms opt in with [data-cart-add-form]. The submit button stays a plain submit button, so
 * without JavaScript the form posts as before and the server redirects.
 *
 * An add changes two things outside the form: the header cart badge and the toast. The badge is
 * returned by the server; the toast is raised through the "cart:toast" event, because both pills
 * are rendered by the page, where the visitor's language is known.
 *
 * Requests go through the shared cart request helper, so an add queues behind any other cart
 * change in flight rather than racing it.
 */
(() => {
    "use strict";

    const TOAST_EVENT = "cart:toast";

    const cartRequest = window.DancingGoat?.cartRequest;

    function showToast(kind) {
        document.dispatchEvent(new CustomEvent(TOAST_EVENT, { detail: { kind } }));
    }

    function add(form, submitter) {
        // Read the form now: the quantity or variant may change before the request runs.
        const body = new FormData(form);
        if (submitter?.name) {
            body.append(submitter.name, submitter.value);
        }

        // Both cart forms can contain a button named "action", which shadows form.action in the DOM.
        const url = new URL(form.getAttribute("action") || location.href, document.baseURI);

        form.dataset.cartAddBusy = "true";
        submitter?.setAttribute("aria-busy", "true");

        cartRequest.enqueue(() => cartRequest.post(url, body)
            .then((parsed) => {
                if (!parsed.querySelector(".cart-badge")) {
                    throw new Error("Unexpected add to cart response.");
                }

                cartRequest.swapBadge(parsed);
                showToast("added");
            })
            .catch((error) => {
                console.error(error);
                showToast("error");
            })
            .finally(() => {
                delete form.dataset.cartAddBusy;
                submitter?.removeAttribute("aria-busy");
            }));
    }

    /*
     * Product cards are rendered by a display template that several pages reuse, so submits are
     * delegated from the document rather than each form being bound. Only forms that opt in are
     * intercepted; every other form on the page keeps posting normally.
     */
    document.addEventListener("submit", (event) => {
        const form = event.target;
        if (!(form instanceof HTMLFormElement) || !form.matches("[data-cart-add-form]") || !cartRequest) {
            return;
        }

        event.preventDefault();

        /*
         * Two guards, for two different mistakes: this one stops an impatient second click on the
         * same button from adding the product twice, while the shared queue stops adds from
         * different cards from interleaving and losing one another.
         */
        if (form.dataset.cartAddBusy) {
            return;
        }

        add(form, event.submitter);
    });
})();
