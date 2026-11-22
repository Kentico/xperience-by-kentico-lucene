import { expect, test } from "@playwright/test";
import { LuceneIndexesPage } from "./luceneIndexesPage";
import { openCoffeeSearchWhenReady } from "../shared/search";

test.describe("Lucene index administration", () => {
  test.afterEach(async ({ page }, testInfo) => {
    const paths = testInfo.annotations.flatMap((annotation) =>
      annotation.type === "lucene-included-path" && annotation.description
        ? [annotation.description]
        : [],
    );
    if (paths.length) {
      await new LuceneIndexesPage(page).restoreIncludedPaths(paths);
    }
  });

  test("lists and opens the configured search index", async ({ page }) => {
    const indexes = new LuceneIndexesPage(page);

    await indexes.goto();
    await indexes.expectIndexListed();
    await indexes.openIndex();

    await expect(page.getByText("Edit index", { exact: true })).toBeVisible();
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

  test("edits, saves, and deletes an included path", async ({
    page,
  }, testInfo) => {
    const indexes = new LuceneIndexesPage(page);
    const originalPath = `/playwright-${Date.now()}`;
    const editedPath = `${originalPath}-edited`;
    testInfo.annotations.push(
      { type: "lucene-included-path", description: originalPath },
      { type: "lucene-included-path", description: editedPath },
    );

    await indexes.goto();
    await indexes.expectIndexListed();
    await indexes.openIndex();
    await indexes.openConfiguredChannel();

    const selectedContentType = await indexes.addIncludedPath(originalPath);
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
    await editedPathRow.getByTestId("button-delete").click();
    await expect(editedPathRow).toHaveCount(0);
    await indexes.saveChannelConfiguration();
    await indexes.saveIndexConfiguration();
    await page.reload();

    await indexes.openConfiguredChannel();
    await expect(indexes.includedPathRow(editedPath)).toHaveCount(0);

    const channelRow = indexes.configuredChannelRow();
    await channelRow.hover();
    await channelRow.getByTestId("button-delete").click();
    await expect(channelRow).toHaveCount(0);

    await page.reload();
    await expect(indexes.configuredChannelRow()).toBeVisible();
  });
});
