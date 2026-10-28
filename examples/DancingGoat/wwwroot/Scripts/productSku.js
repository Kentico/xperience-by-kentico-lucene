/*
 * Product SKU — mirrors the SKU of the variant selected in the buy card.
 * Each variant option carries its code in [data-sku]; the line under the card
 * opts in with [data-product-sku]. Products without variants render their SKU
 * server-side and this script leaves them alone.
 */
(function () {
    "use strict";

    const target = document.querySelector("[data-product-sku]");
    const select = document.getElementById("variantId");

    if (!target || !select || target.dataset.productSkuInitialized) {
        return;
    }
    target.dataset.productSkuInitialized = "true";

    function update() {
        const sku = select.selectedOptions[0]?.dataset.sku ?? "";

        target.textContent = sku;
        target.hidden = sku === "";
    }

    select.addEventListener("change", update);
    update();
})();
