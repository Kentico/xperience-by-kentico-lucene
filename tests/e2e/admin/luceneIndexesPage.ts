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

  async openConfiguredChannel(): Promise<void> {
    await this.configuredChannelRow().click();
    await expect(
      this.page.getByRole("button", {
        name: "Save website channel configuration",
      }),
    ).toBeVisible();
  }

  configuredChannelRow(): Locator {
    const channelTable = this.page.getByRole("table").filter({
      has: this.page.getByRole("columnheader", {
        name: "Website Channel",
      }),
    });

    return channelTable.getByRole("row").nth(1);
  }

  includedPathRow(path: string): Locator {
    const pathTable = this.page.getByRole("table").filter({
      has: this.page.getByRole("columnheader", { name: "Path" }),
    });

    return pathTable.getByRole("row").filter({ hasText: path });
  }

  async addIncludedPath(path: string): Promise<string> {
    await this.page.getByRole("button", { name: "Add new path" }).click();
    await this.page.getByLabel("Included Path").fill(path);

    const contentTypes = this.page.getByPlaceholder("Select content types");
    await contentTypes.click();
    const firstOption = this.page.getByRole("option").first();
    await expect(firstOption).toBeVisible();
    const contentType = await firstOption.innerText();
    await firstOption.click();

    await this.page.getByRole("button", { name: "Save Path" }).click();
    await expect(this.includedPathRow(path)).toBeVisible();
    return contentType;
  }

  async editIncludedPath(
    path: string,
    newPath: string,
    expectedContentType: string,
  ): Promise<void> {
    await this.includedPathRow(path).getByRole("cell").first().click();
    const pathInput = this.page.getByLabel("Included Path");
    await expect(pathInput).toHaveValue(path);
    await expect(
      this.page.getByText(expectedContentType, { exact: true }),
    ).toBeVisible();
    await pathInput.fill(newPath);
    await this.page.getByRole("button", { name: "Save Path" }).click();
    await expect(this.includedPathRow(newPath)).toBeVisible();
  }

  async saveChannelConfiguration(): Promise<void> {
    await this.page
      .getByRole("button", {
        name: "Save website channel configuration",
      })
      .click();
    await expect(
      this.page.getByRole("button", {
        name: "Add new website channel configuration",
      }),
    ).toBeVisible();
  }

  async saveIndexConfiguration(): Promise<void> {
    await this.page.getByRole("button", { name: "Save", exact: true }).click();
    await expect(
      this.page.getByText("Index edited", { exact: true }),
    ).toBeVisible();
  }

  async restoreIncludedPaths(paths: string[]): Promise<void> {
    await this.goto();
    await this.expectIndexListed();
    await this.openIndex();
    await this.openConfiguredChannel();

    let removedPath = false;
    for (const path of paths) {
      const pathRow = this.includedPathRow(path);
      if (await pathRow.count()) {
        await pathRow.hover();
        await pathRow.getByRole("button", { name: /delete/i }).click();
        removedPath = true;
      }
    }

    if (removedPath) {
      await this.saveChannelConfiguration();
      await this.saveIndexConfiguration();
    }
  }

  async rebuildIndex(): Promise<void> {
    await this.indexRow.getByRole("checkbox").check();
    await this.page.getByRole("button", { name: "Rebuild" }).click();
    await expect(this.page.getByText(/Indexing in progress\./)).toBeVisible();
  }
}
