/*
 * Checkout address blocks — loads the state/province list for the selected
 * country and hides the group when the country has none. Also drives the
 * "same as billing" toggle that collapses the shipping block.
 *
 * Each block opts in with [data-address-fields] and carries its element ids and
 * endpoint in data-* attributes, so the billing and shipping instances are
 * handled by the same code.
 */
(function () {
    "use strict";

    const setGroupVisible = (group, visible) => {
        if (group) {
            group.style.display = visible ? "block" : "none";
        }
    };

    const resetOptions = (dropdown) => {
        while (dropdown.options.length > 1) {
            dropdown.remove(1);
        }
    };

    document.querySelectorAll("[data-address-fields]").forEach((fields) => {
        const statesGroup = document.getElementById(fields.dataset.stateGroup);
        const statesDropdown = document.getElementById(fields.dataset.stateDropdown);
        const countryDropdown = document.getElementById(fields.dataset.countryDropdown);
        const statesUrl = fields.dataset.statesUrl;
        const storagePrefix = fields.dataset.storagePrefix;

        if (!statesDropdown || !countryDropdown || !statesUrl) {
            return;
        }

        const stateKey = storagePrefix + "Address_State";

        setGroupVisible(statesGroup, statesDropdown.options.length > 1);

        countryDropdown.addEventListener("change", () => {
            fetch(statesUrl, {
                method: "POST",
                headers: { "Content-Type": "application/x-www-form-urlencoded" },
                body: new URLSearchParams({ CountryId: countryDropdown.value }).toString()
            })
                .then((response) => {
                    if (!response.ok) {
                        throw new Error("Network response was not ok.");
                    }
                    return response.json();
                })
                .then((states) => {
                    resetOptions(statesDropdown);

                    states.forEach((state) => {
                        const option = document.createElement("option");
                        option.text = state.text;
                        option.value = state.value;
                        statesDropdown.appendChild(option);
                    });

                    let saved = null;
                    try {
                        saved = window.localStorage.getItem(storagePrefix + "Address_StateId");
                    } catch { }

                    if (states.some((state) => state.value === saved)) {
                        statesDropdown.value = saved;
                        try {
                            window.localStorage.setItem(stateKey, statesDropdown.options[statesDropdown.selectedIndex].text);
                        } catch { }
                    } else {
                        try {
                            window.localStorage.removeItem(stateKey);
                        } catch { }
                    }

                    setGroupVisible(statesGroup, statesDropdown.options.length > 1);

                    // The state list (and programmatic selection) changed without user input;
                    // announce it so listeners such as the order summary refresh with the
                    // resolved country + state pair instead of the stale one.
                    statesDropdown.dispatchEvent(new Event("change", { bubbles: true }));
                })
                .catch((error) => console.error("Fetch error:", error));
        });
    });

    document.querySelectorAll("[data-shipping-toggle]").forEach((toggle) => {
        const fields = document.getElementById(toggle.dataset.shippingToggle);
        if (!fields) {
            return;
        }

        toggle.addEventListener("change", () => {
            fields.style.display = toggle.checked ? "none" : "block";
        });
    });
})();
