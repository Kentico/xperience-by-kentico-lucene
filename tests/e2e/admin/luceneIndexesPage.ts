import { expect, type Locator, type Page } from "@playwright/test";
import { adminBaseUrl } from "../shared/config";

export const luceneIndexName = "DancingGoatGlobal";

export class LuceneIndexesPage {
  constructor(private readonly page: Page) {}

  private get indexRow(): Locator {
    return this.page.getByRole("row").filter({ hasText: luceneIndexName });
  }

  async goto(): Promise<void> {
    await this.page.goto(`${adminBaseUrl}/lucene/indexes`);
  }

  async expectIndexListed(): Promise<void> {
    await expect(
      this.page.getByText(luceneIndexName, { exact: true }),
    ).toBeVisible({
      timeout: 30_000,
    });
    await expect(this.indexRow).toBeVisible();
  }

  async openIndex(): Promise<void> {
    const editAction = this.indexRow.getByRole("button", { name: /edit/i });
    await editAction.click();
    await expect(this.page.getByLabel("Index Name")).toHaveValue(
      luceneIndexName,
    );
  }

  async rebuildIndex(): Promise<void> {
    await this.indexRow.getByRole("checkbox").check();
    await this.page.getByRole("button", { name: "Rebuild" }).click();
    await expect(this.page.getByText(/Indexing in progress\./)).toBeVisible();
  }
}
