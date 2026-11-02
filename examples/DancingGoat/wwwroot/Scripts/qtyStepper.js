/*
 * Quantity stepper — the minus/plus buttons around a number input.
 * Containers opt in with [data-qty-stepper]; buttons carry [data-qty-step].
 *
 * On the cart page the stepper also carries [data-qty-submit]. There the row form is posted
 * in the background after a short pause, so a burst of clicks results in a single request and
 * the page neither reloads nor scrolls: the server returns the two regions a cart change can
 * affect and they are swapped in place. The submit button stays in the markup and is only
 * hidden by this script, so the form still works as a plain post without JavaScript.
 *
 * Elsewhere (the product detail page) the stepper only adjusts the input.
 */
(function () {
    "use strict";

    const SUBMIT_DELAY = 450;
    const UPDATED_EVENT = "cart:updated";

    const cartRequest = window.DancingGoat?.cartRequest;

    function toInt(value, fallback) {
        const parsed = parseInt(value, 10);

        return Number.isNaN(parsed) ? fallback : parsed;
    }

    function clamp(input, value) {
        const min = toInt(input.min, 1);
        const max = toInt(input.max, Number.MAX_SAFE_INTEGER);

        return Math.max(min, Math.min(max, value));
    }

    function attributeSelector(name, value) {
        return "[" + name + '="' + value.replace(/["\\]/g, "\\$&") + '"]';
    }

    const cart = document.querySelector("[data-cart]");

    // Ordering lives in the shared cart request helper; this only tracks what looks busy on screen.
    let inFlight = 0;

    // Stepper submits waiting out their pause, so they can be sent early when something else changes the cart.
    const scheduled = new Set();

    function schedule(run) {
        const entry = { run };

        entry.timer = window.setTimeout(() => {
            scheduled.delete(entry);
            run();
        }, SUBMIT_DELAY);
        scheduled.add(entry);

        return entry;
    }

    function unschedule(entry) {
        if (entry) {
            window.clearTimeout(entry.timer);
            scheduled.delete(entry);
        }
    }

    /*
     * Sends every waiting stepper submit now, reading the quantities still on screen. Dropping those
     * timers instead would silently discard a quantity the customer has already typed.
     */
    function flushScheduled() {
        const entries = [...scheduled];

        scheduled.clear();
        entries.forEach((entry) => {
            window.clearTimeout(entry.timer);
            entry.run();
        });
    }

    function enqueue(task) {
        inFlight += 1;
        cart?.setAttribute("aria-busy", "true");

        cartRequest.enqueue(task).finally(() => {
            inFlight -= 1;
            if (inFlight === 0) {
                cart?.removeAttribute("aria-busy");
            }
        });
    }

    function showError() {
        const message = cart?.dataset.cartError;
        const region = document.querySelector("[data-cart-lines]");
        if (!region || !message) {
            return;
        }

        // Inside the lines region, not in .cart-layout itself, which is the two-column grid.
        let alert = region.querySelector("[data-cart-alert]");
        if (!alert) {
            alert = document.createElement("p");
            alert.setAttribute("data-cart-alert", "");
            alert.setAttribute("role", "alert");
            alert.className = "cart-alert";
            region.prepend(alert);
        }
        alert.textContent = message;
    }

    // The message describes the action, so it comes from the control that was used.
    function announce(message) {
        const status = document.getElementById("cartStatus");
        if (!status || !message) {
            return;
        }

        const total = document.querySelector(".sum-row.total")?.textContent.replace(/\s+/g, " ").trim() ?? "";

        status.textContent = total ? message + " " + total : message;
    }

    function swapRegion(source, selector) {
        const incoming = source.querySelector(selector);
        const live = document.querySelector(selector);
        if (incoming && live) {
            live.replaceChildren(...incoming.childNodes);
        }
    }

    /*
     * The promo/discount panel sits inside the region that is replaced, so an open panel and a
     * half-typed code are carried across the swap. Applying a code is the exception: there the
     * server decides what the panel shows, so the markup it returns wins.
     */
    function capturePromotionState() {
        const container = document.querySelector(".cart-promotion-code");
        if (!container) {
            return null;
        }

        const input = container.querySelector(".cart-promotion-code-input");

        return {
            open: container.querySelector("[data-promotion-toggle]")?.getAttribute("aria-expanded") === "true",
            value: input?.value ?? "",
            focused: document.activeElement === input
        };
    }

    // Returns the input to put focus back into, or null when it did not have focus.
    function restorePromotionState(state) {
        const container = state && document.querySelector(".cart-promotion-code");
        if (!container) {
            return null;
        }

        if (state.open) {
            container.dataset.promotionOpen = "true";
        }

        const input = container.querySelector(".cart-promotion-code-input");
        if (input && state.value) {
            input.value = state.value;
        }

        return state.focused ? input : null;
    }

    /*
     * The control the customer used is replaced along with its region, so focus goes back to the
     * freshly rendered copy, found by its stable key. When that control is gone — its row was
     * removed — focus falls to a neighbouring row and finally to the promo code toggle, rather
     * than dropping to the document body.
     */
    function focusSelectors(qtyKey, source, focusKey, focusGroup) {
        const selectors = [];

        if (qtyKey) {
            const stepper = attributeSelector("data-qty-key", qtyKey);

            selectors.push(source === "input"
                ? stepper + " input"
                : stepper + " " + attributeSelector("data-qty-step", source));
        }

        if (focusKey) {
            selectors.push(attributeSelector("data-cart-focus", focusKey));
        }

        /*
         * Neighbours are only ever looked for inside the control's own component: a removed cart line
         * hands focus to another line, a removed code to another code. Reaching across would put focus
         * on a control that deletes something else, and the next Enter would fire it. A control with no
         * group - the Apply button, which is hidden once the panel collapses - falls straight through
         * to the toggle below.
         */
        if (focusKey && focusGroup) {
            const keys = [...document.querySelectorAll(attributeSelector("data-cart-focus-group", focusGroup))]
                .map((element) => element.dataset.cartFocus);
            const index = keys.indexOf(focusKey);

            if (index >= 0) {
                [keys[index + 1], keys[index - 1]]
                    .filter((neighbour) => neighbour)
                    .forEach((neighbour) => selectors.push(attributeSelector("data-cart-focus", neighbour)));
            }
        }

        selectors.push("[data-promotion-toggle]");

        return selectors;
    }

    function focusFirst(selectors) {
        for (const selector of selectors) {
            const element = document.querySelector(selector);
            if (!element) {
                continue;
            }

            element.focus();

            // A control inside the collapsed promo panel is hidden, so focusing it does nothing.
            if (document.activeElement === element) {
                return;
            }
        }
    }

    function update(form, submitter, qtyKey, source) {
        const announcement = submitter?.dataset.cartAnnounce;
        const focusKey = submitter?.dataset.cartFocus;
        const focusGroup = submitter?.dataset.cartFocusGroup;
        const keepsPromotionState = !submitter?.hasAttribute("data-cart-promotion-submit");

        // Read the form now: by the time the request runs, its markup may already have been replaced.
        const body = new FormData(form);
        if (submitter?.name) {
            body.append(submitter.name, submitter.value);
        }

        // Both cart forms contain a button named "action", which shadows form.action in the DOM.
        const url = new URL(form.getAttribute("action") || location.href, document.baseURI);

        enqueue(() => cartRequest.post(url, body)
            .then((parsed) => {
                // The last line was removed: the empty-cart screen replaces the whole page, so swapping regions is not enough.
                if (parsed.querySelector("[data-cart-empty]")) {
                    window.location.reload();
                    return;
                }

                if (!parsed.querySelector("[data-cart-lines]")) {
                    throw new Error("Unexpected cart response.");
                }

                // Anything still waiting out its pause is sent before its markup disappears.
                flushScheduled();

                const promotionState = keepsPromotionState ? capturePromotionState() : null;
                const selectors = focusSelectors(qtyKey, source, focusKey, focusGroup);

                swapRegion(parsed, "[data-cart-lines]");
                swapRegion(parsed, "[data-cart-totals]");
                cartRequest.swapBadge(parsed);

                document.querySelector("[data-cart-alert]")?.remove();
                bindSteppers();

                const promotionInput = restorePromotionState(promotionState);

                // The promo code panel binds itself to the markup that has just arrived.
                document.dispatchEvent(new CustomEvent(UPDATED_EVENT));

                if (promotionInput) {
                    promotionInput.focus();
                    promotionInput.setSelectionRange(promotionInput.value.length, promotionInput.value.length);
                } else {
                    focusFirst(selectors);
                }

                // A rejected code renders its own role="alert" message; announcing success after it would contradict it.
                const rejected = !keepsPromotionState && document.querySelector("[data-cart-lines] .cart-promotion-code-error");

                announce(rejected ? null : announcement);
            })
            .catch((error) => {
                console.error(error);
                showError();
            }));
    }

    function bindSteppers() {
        document.querySelectorAll("[data-qty-stepper]").forEach((stepper) => {
            if (stepper.dataset.qtyStepperInitialized) {
                return;
            }
            stepper.dataset.qtyStepperInitialized = "true";

            const input = stepper.querySelector("input");
            if (!input) {
                return;
            }

            const submits = stepper.hasAttribute("data-qty-submit");
            const form = submits ? stepper.closest("form") : null;
            const submitter = form ? form.querySelector("[data-qty-submitter]") : null;
            let entry;

            // Without the cart request helper nothing posts in the background, so the button stays.
            if (submitter && cartRequest) {
                submitter.hidden = true;
            }

            function scheduleUpdate(source) {
                if (!form || !cartRequest) {
                    return;
                }

                unschedule(entry);
                entry = schedule(() => update(form, submitter, stepper.dataset.qtyKey, source));
            }

            function currentValue() {
                return toInt(input.value, toInt(input.min, 1));
            }

            stepper.querySelectorAll("[data-qty-step]").forEach((button) => {
                button.addEventListener("click", () => {
                    const current = currentValue();
                    const next = clamp(input, current + toInt(button.dataset.qtyStep, 0));

                    if (next === current) {
                        return;
                    }

                    input.value = next;
                    scheduleUpdate(button.dataset.qtyStep);
                });
            });

            input.addEventListener("change", () => {
                input.value = clamp(input, currentValue());
                scheduleUpdate("input");
            });
        });
    }

    /*
     * Removing a line and applying a code go through the same background update, so the whole
     * cart is delegated rather than each form being bound: the markup is replaced on every change.
     * Only forms that mutate the cart opt in, so anything else placed in the cart keeps posting normally.
     */
    cart?.addEventListener("submit", (event) => {
        const form = event.target;
        if (!(form instanceof HTMLFormElement) || !form.matches("[data-cart-form]") || !cartRequest) {
            return;
        }

        event.preventDefault();

        /*
         * Enter in the quantity field submits through the stepper's own hidden button. The change
         * handler has already scheduled that update, so handling it again here would post the same
         * quantity twice and move focus out of the field the customer is still in.
         */
        if (event.submitter?.hasAttribute("data-qty-submitter")) {
            return;
        }

        // A quantity change still waiting out its pause goes first, so this response does not overwrite it.
        flushScheduled();
        update(form, event.submitter, null, null);
    });

    bindSteppers();
})();
