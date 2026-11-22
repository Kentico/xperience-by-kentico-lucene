import { expect, test } from "@playwright/test";
import { LuceneIndexesPage, luceneIndexName } from "./luceneIndexesPage";

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

  test("starts a rebuild from the index listing", async ({ page }) => {
    const indexes = new LuceneIndexesPage(page);

    await indexes.goto();
    await indexes.expectIndexListed();
    await indexes.rebuildIndex();
    await expect(
      page.getByText(luceneIndexName, { exact: true }),
    ).toBeVisible();
  });
});
