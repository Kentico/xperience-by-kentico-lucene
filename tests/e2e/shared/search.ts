import { expect, type Page } from "@playwright/test";

export async function openCoffeeSearchWhenReady(page: Page): Promise<void> {
  await expect
    .poll(
      async () => {
        await page.goto("/search?query=coffee&pageSize=1");
        return page.locator(".search-result").count();
      },
      {
        timeout: 60_000,
        intervals: [1_000, 2_000, 5_000],
        message:
          "The startup search index rebuild did not produce coffee results.",
      },
    )
    .toBeGreaterThan(0);
}
