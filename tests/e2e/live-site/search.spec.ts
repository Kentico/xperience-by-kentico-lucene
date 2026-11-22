import { expect, test } from "@playwright/test";
import { openCoffeeSearchWhenReady } from "../shared/search";

test.describe("DancingGoat Lucene search", () => {
  test("waits for coffee search results and pages through them", async ({
    page,
  }) => {
    await openCoffeeSearchWhenReady(page);

    await expect(page.locator(".search-count")).toContainText(/\d+ results/);
    await expect(page.locator(".search-result")).toHaveCount(1);
    await expect(
      page.getByRole("navigation", { name: "Search result pages" }),
    ).toBeVisible();

    await page.getByRole("link", { name: "Next" }).click();
    await expect(page).toHaveURL(/[?&]page=2(?:&|$)/);
    await expect(page.locator(".search-result")).toHaveCount(1);
  });

  test("filters coffee results by content type and taxonomy facet", async ({
    page,
  }) => {
    await openCoffeeSearchWhenReady(page);
    await page.getByRole("link", { name: "Products" }).click();
    await expect(page.getByRole("link", { name: "Products" })).toHaveAttribute(
      "aria-current",
      "true",
    );

    const facetOptions = page.locator(
      "details.search-facet ul.search-facet-options a",
    );
    await expect(facetOptions.nth(1)).toBeVisible();
    await facetOptions.nth(1).click();

    await expect(page.locator(".search-active-filters")).toBeVisible();
    await expect(page.locator(".search-filter-chip").first()).toBeVisible();
    await expect(page.locator(".search-result").first()).toBeVisible();
  });

  test("serves the localized Spanish search page", async ({ page }) => {
    const response = await page.goto("/es/buscar?query=caf%C3%A9");

    expect(response?.status()).toBe(200);
    await expect(page.getByRole("heading", { level: 1 })).not.toHaveText(
      "Search",
    );
    await expect(page.getByRole("searchbox")).toHaveValue("café");
  });
});
