import { expect, test } from "@playwright/test";
import { LuceneIndexesPage } from "../admin/luceneIndexesPage";
import { capture } from "../shared/screenshot";

test("Lucene index listing", async ({ page }) => {
  const indexes = new LuceneIndexesPage(page);

  await indexes.goto();
  await indexes.expectIndexListed();
  await expect(
    page.getByText("DancingGoatGlobal", { exact: true }),
  ).toBeVisible();
  await capture(page, "lucene-index-listing", { cropHeight: 700 });
});
