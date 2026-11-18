/*
 * Cart request — the one definition of how the site posts a cart change in the background, shared
 * by the store's add-to-cart and the cart page's quantity, remove and promotion-code forms.
 *
 * Cart mutations run one at a time. Every one of them is a read-modify-write of the same session
 * cart, so two in flight together can lose one, and responses arriving out of order can leave the
 * header badge showing a count that was already superseded. Aborting is not an option either: the
 * server has already made the change by the time a response is discarded.
 *
 * Callers own what the customer sees - which control looks busy, what is announced, which regions
 * are swapped. This file owns the contract with the server and the ordering.
 *
 * A rejected antiforgery token reloads the page instead of failing, as no request from a page whose
 * tokens have expired can succeed.
 */
(() => {
    "use strict";

    const UPDATE_HEADER = "X-Cart-Update";
    const REQUEST_TIMEOUT = 15000;

    let queue = Promise.resolve();

    /*
     * Runs tasks in the order they were queued, whichever script queued them. A failed task must not
     * stop the ones behind it, so the chain swallows rejections; the task keeps its own result.
     */
    function enqueue(task) {
        const run = queue.then(task, task);

        queue = run.catch(() => { });

        return run;
    }

    /*
     * Posts a cart form and hands back the response parsed as a document. A redirect means the
     * server answered the no-JavaScript way, which is not something a caller can swap in.
     */
    function post(url, body) {
        return fetch(url, {
            method: "POST",
            headers: { [UPDATE_HEADER]: "1" },
            body,
            signal: AbortSignal.timeout(REQUEST_TIMEOUT)
        })
            .then((response) => {
                /*
                 * A 400 means the antiforgery token was rejected, typically after the page sat idle
                 * past the cookie's lifetime. Every token on the page is stale then, so retrying cannot
                 * succeed; a reload issues fresh ones. The promise never settles, so the caller does
                 * not flash an error for the moment before the page goes away.
                 */
                if (response.status === 400) {
                    window.location.reload();

                    return new Promise(() => { });
                }

                const contentType = (response.headers.get("Content-Type") || "").split(";")[0].trim();
                if (!response.ok || response.redirected || contentType !== "text/html") {
                    throw new Error("The cart could not be updated.");
                }

                return response.text();
            })
            .then((html) => new DOMParser().parseFromString(html, "text/html"));
    }

    // The badge is a leaf element outside every swapped region, so it is replaced rather than filled.
    function swapBadge(parsed) {
        const incoming = parsed.querySelector(".cart-badge");
        const live = document.querySelector(".cart-badge");

        if (incoming && live) {
            live.replaceWith(incoming);
        }
    }

    window.DancingGoat = window.DancingGoat || {};
    window.DancingGoat.cartRequest = { enqueue, post, swapBadge };
})();
