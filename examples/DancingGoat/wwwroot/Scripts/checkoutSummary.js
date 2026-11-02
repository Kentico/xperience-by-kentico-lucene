/*
 * Checkout order summary — keeps the shipping and tax rows of the dark summary
 * card in step with the selected shipping method and billing address, without
 * posting the whole checkout form. Progressive enhancement: the card is already
 * rendered server side, this only refreshes it.
 */
(function () {
    "use strict";

    const form = document.getElementById("checkoutForm");
    const summary = document.getElementById("checkoutSummary");
    if (!form || !summary) {
        return;
    }

    const summaryUrl = summary.dataset.summaryUrl;
    if (!summaryUrl) {
        return;
    }

    const status = document.getElementById("checkoutSummaryStatus");
    let pending;

    function showError() {
        const message = summary.dataset.summaryError;
        if (!message) {
            return;
        }

        let alert = summary.querySelector("[data-summary-alert]");
        if (!alert) {
            alert = document.createElement("p");
            alert.setAttribute("data-summary-alert", "");
            alert.setAttribute("role", "alert");
            alert.className = "summary-alert";
            summary.prepend(alert);
        }
        alert.textContent = message;
    }

    function clearError() {
        summary.querySelector("[data-summary-alert]")?.remove();
    }

    function announce() {
        if (!status) {
            return;
        }

        const prefix = summary.dataset.summaryUpdated || "";
        const total = summary.querySelector(".sum-row.total")?.textContent.replace(/\s+/g, " ").trim() ?? "";

        status.textContent = total ? prefix + " " + total : prefix;
    }

    function refresh() {
        const request = new AbortController();
        if (pending) {
            pending.abort();
        }
        pending = request;
        summary.setAttribute("aria-busy", "true");

        fetch(summaryUrl, {
            method: "POST",
            headers: { "Content-Type": "application/x-www-form-urlencoded" },
            body: new URLSearchParams(new FormData(form)).toString(),
            signal: request.signal
        })
            .then((response) => {
                const contentType = (response.headers.get("Content-Type") || "").split(";")[0].trim();
                if (!response.ok || response.redirected || contentType !== "text/html") {
                    throw new Error("Order summary could not be refreshed.");
                }
                return response.text();
            })
            .then((html) => {
                const card = new DOMParser().parseFromString(html, "text/html").querySelector("[data-order-summary]");
                if (!card) {
                    throw new Error("Unexpected order summary response.");
                }

                summary.replaceChildren(card);
                clearError();
                announce();
            })
            .catch((error) => {
                if (error.name !== "AbortError") {
                    console.error(error);
                    showError();
                }
            })
            .finally(() => {
                if (pending === request) {
                    pending = null;
                    summary.removeAttribute("aria-busy");
                }
            });
    }

    // A billing country change is intentionally not listened to: checkoutAddress.js reloads
    // the state list for the new country and dispatches "change" on the state dropdown once
    // the selection is resolved, so refreshing here would post a stale country + state pair.
    ["shippingDropdown", "paymentDropdown", "stateDropdownBilling"].forEach((id) => {
        document.getElementById(id)?.addEventListener("change", refresh);
    });
})();
