import { expect, test } from "@playwright/test";
import { LuceneIndexesPage } from "./luceneIndexesPage";
import { openCoffeeSearchWhenReady } from "../shared/search";

test.describe("Lucene index administration", () => {
  test("lists and opens the configured search index", async ({ page }) => {
    const indexes = new LuceneIndexesPage(page);

    await indexes.goto();
    await indexes.expectIndexListed();
    await indexes.openIndex();

    await expect(
      page.getByRole("heading", { name: "Edit index" }),
    ).toBeVisible();
  });

  test("rebuilds the index and returns coffee search results", async ({
    page,
  }) => {
    const indexes = new LuceneIndexesPage(page);

    await indexes.goto();
    await indexes.expectIndexListed();
    await indexes.rebuildIndex();
    await openCoffeeSearchWhenReady(page);
    await expect(page.locator(".search-result")).toHaveCount(1);
  });
});
