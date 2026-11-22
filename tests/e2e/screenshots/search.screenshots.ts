import { expect, test } from "@playwright/test";
import { capture } from "../shared/screenshot";
import { openCoffeeSearchWhenReady } from "../shared/search";

test("DancingGoat search results", async ({ page }) => {
  await openCoffeeSearchWhenReady(page);
  await expect(page.locator(".search-result").first()).toBeVisible();
  await capture(page, "dancing-goat-search-results", { fullHeight: 6000 });
});
