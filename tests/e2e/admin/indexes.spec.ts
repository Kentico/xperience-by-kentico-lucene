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

  test("edits, saves, and deletes an included path", async ({ page }) => {
    const indexes = new LuceneIndexesPage(page);
    const originalPath = `/playwright-${Date.now()}`;
    const editedPath = `${originalPath}-edited`;
    let selectedContentType: string;

    await indexes.goto();
    await indexes.expectIndexListed();
    await indexes.openIndex();
    await indexes.openConfiguredChannel();

    try {
      selectedContentType = await indexes.addIncludedPath(originalPath);
      await indexes.saveChannelConfiguration();
      await indexes.saveIndexConfiguration();
      await page.reload();

      await indexes.openConfiguredChannel();
      await expect(indexes.includedPathRow(originalPath)).toBeVisible();
      await indexes.editIncludedPath(
        originalPath,
        editedPath,
        selectedContentType,
      );
      await indexes.saveChannelConfiguration();
      await indexes.saveIndexConfiguration();
      await page.reload();

      await indexes.openConfiguredChannel();
      await expect(indexes.includedPathRow(editedPath)).toBeVisible();
      const editedPathRow = indexes.includedPathRow(editedPath);
      await editedPathRow.hover();
      await editedPathRow.getByRole("button", { name: /delete/i }).click();
      await expect(editedPathRow).toHaveCount(0);
      await indexes.saveChannelConfiguration();
      await indexes.saveIndexConfiguration();
      await page.reload();

      await indexes.openConfiguredChannel();
      await expect(indexes.includedPathRow(editedPath)).toHaveCount(0);

      const channelRow = indexes.configuredChannelRow();
      await channelRow.hover();
      await channelRow.getByRole("button", { name: /delete/i }).click();
      await expect(channelRow).toHaveCount(0);

      await page.reload();
      await expect(indexes.configuredChannelRow()).toBeVisible();
    } finally {
      await indexes.restoreIncludedPaths([originalPath, editedPath]);
    }
  });
});
