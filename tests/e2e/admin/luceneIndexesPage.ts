import { expect, type Locator, type Page } from "@playwright/test";
import { adminBaseUrl } from "../shared/config";

export const luceneIndexName = "DancingGoatGlobal";

export class LuceneIndexesPage {
  constructor(private readonly page: Page) {}

  private get indexRow(): Locator {
    // Match the exact code name cell; other indexes may use the DancingGoatGlobalSearch strategy.
    return this.page.getByRole("row").filter({
      has: this.page.getByRole("cell", { name: luceneIndexName, exact: true }),
    });
  }

  async goto(): Promise<void> {
    await this.page.goto(`${adminBaseUrl}/lucene/indexes`);
  }

  async openWizard(): Promise<void> {
    await this.page.goto(`${adminBaseUrl}/lucene/indexes/create`);
    await expect(this.page.getByTestId("step-identity")).toBeVisible();
  }

  async addWizardChannel(): Promise<void> {
    await this.page
      .getByRole("button", { name: "Add website channel", exact: true })
      .click();
    const form = this.page.getByTestId("wizard-source-form");
    await expect(form).toBeVisible();
    await form.getByLabel("Website channel", { exact: true }).click();
    await this.page.getByTestId("wizard-channel-option").first().click();
    await form.getByRole("textbox", { name: /Path$/ }).click({ trial: true });
  }

  async expectWizardStep(label: string): Promise<void> {
    const current = this.page
      .getByTestId("wizard-steps")
      .locator('[aria-current="step"]');
    await expect(current).toHaveCount(1);
    await expect(current).toHaveText(label);
    const currentBounds = await current.boundingBox();
    const buttonBounds = await current.getByRole("button").boundingBox();
    if (!currentBounds || !buttonBounds) {
      throw new Error("The current wizard step must have visible bounds.");
    }
    expect(currentBounds.width).toBeLessThanOrEqual(buttonBounds.width + 1);
  }

  async selectWizardCheckbox(group: string): Promise<void> {
    const checkbox = this.page
      .getByRole("group", { name: group, exact: true })
      .getByRole("checkbox")
      .first();
    await checkbox.press("Space");
    await expect(checkbox).toBeChecked();
  }

  async expectIndexListed(): Promise<void> {
    await expect(
      this.indexRow
        .getByRole("cell", { name: luceneIndexName, exact: true })
        .first(),
    ).toBeVisible({
      timeout: 30_000,
    });
    await expect(this.indexRow).toBeVisible();
  }

  async openIndex(): Promise<void> {
    await this.indexRow.getByRole("link").first().click();
    await this.openTab("Configuration");
    await expect(
      this.page
        .getByTestId("config-settings")
        .getByText(luceneIndexName, { exact: true })
        .last(),
    ).toBeVisible();
  }

  async openInspector(tab: string): Promise<void> {
    await this.indexRow.getByRole("link").first().click();
    await this.openTab(tab);
  }

  async openTab(tab: string): Promise<void> {
    await this.page
      .getByTestId("side-navigation-vertical-menu")
      .getByRole("link", { name: tab, exact: true })
      .click();
  }

  // Read-only configuration hierarchy and section editor panel helpers.
  configuredChannel(channelName = "DancingGoatPages"): Locator {
    return this.page.locator(
      `[data-testid="config-channel"][data-channel="${channelName}"]`,
    );
  }

  async pathValues(channelName = "DancingGoatPages"): Promise<string[]> {
    const paths =
      this.configuredChannel(channelName).getByTestId("config-path");
    await paths.first().waitFor();
    return paths.evaluateAll((items) =>
      items.map((item) => item.getAttribute("data-path") ?? ""),
    );
  }

  configPanel(): Locator {
    return this.page.getByTestId("config-panel");
  }

  async openSectionEditor(editLabel: string): Promise<Locator> {
    await this.page
      .getByRole("button", { name: editLabel, exact: true })
      .click();
    await expect(this.configPanel()).toHaveCount(1);
    await expect(this.configPanel()).toBeVisible();
    // Wait for the side panel to finish sliding in.
    await this.saveButton().click({ trial: true });
    return this.configPanel();
  }

  async openChannelEditor(channelName = "DancingGoatPages"): Promise<Locator> {
    await this.configuredChannel(channelName)
      .getByRole("button", { name: /^Edit / })
      .click();
    await expect(this.configPanel()).toBeVisible();
    await this.saveButton().click({ trial: true });
    return this.configPanel();
  }

  panelPaths(): Locator {
    return this.configPanel().getByTestId("config-panel-path");
  }

  async panelPathValues(): Promise<string[]> {
    return this.panelPaths()
      .getByRole("textbox", { name: "Path" })
      .evaluateAll((inputs) =>
        inputs.map((input) => (input as HTMLInputElement).value),
      );
  }

  async includedPath(path: string): Promise<Locator> {
    const index = (await this.panelPathValues()).indexOf(path);
    if (index < 0) {
      throw new Error(`The path ${path} is not in the editor.`);
    }
    return this.panelPaths().nth(index);
  }

  async addIncludedPath(path: string): Promise<string> {
    const panel = await this.openChannelEditor();
    await panel
      .getByRole("button", { name: "Add included path", exact: true })
      .click();
    const block = this.panelPaths().last();
    await block.getByRole("textbox", { name: "Path" }).fill(path);
    const selector = block.getByTestId("path-content-types");
    await selector.getByRole("textbox", { name: /^Search / }).click();
    const firstOption = this.contentTypeOptions().first();
    const label = (await firstOption.innerText()).split("\n")[0].trim();
    await this.selectContentType(selector, label);
    return label;
  }

  async editIncludedPath(path: string, newPath: string): Promise<void> {
    await this.openChannelEditor();
    const block = await this.includedPath(path);
    const input = block.getByRole("textbox", { name: "Path" });
    await input.fill(newPath);
    await expect(input).toHaveValue(newPath);
  }

  async removeIncludedPath(path: string): Promise<void> {
    if (!(await this.configPanel().isVisible())) {
      await this.openChannelEditor();
    }
    const block = await this.includedPath(path);
    await block.getByRole("button", { name: /^Remove included path/ }).click();
    expect(await this.panelPathValues()).not.toContain(path);
  }

  saveButton(): Locator {
    return this.page.getByRole("button", { name: "Save", exact: true });
  }

  async saveIndexConfiguration(): Promise<void> {
    await this.saveButton().click();
    await expect(
      this.page
        .getByText(/^Index edited\. Saving does not rebuild the index\./)
        .first(),
    ).toBeVisible();
    await expect(this.configPanel()).toHaveCount(0);
  }

  async restoreIncludedPaths(paths: string[]): Promise<void> {
    await this.goto();
    await this.expectIndexListed();
    await this.openIndex();
    const configured = await this.pathValues();
    const stale = paths.filter((path) => configured.includes(path));
    if (stale.length === 0) {
      return;
    }
    await this.openChannelEditor();
    for (const path of stale) {
      await this.removeIncludedPath(path);
    }
    await this.saveIndexConfiguration();
  }
  // Content type selector helpers (owned by Configuration; reused by the wizard).
  contentTypeSelector(scope: Locator, label: string): Locator {
    return scope.getByRole("group", { name: label, exact: true });
  }

  // The option list is a native drop-down rendered outside the selector; only one is open at a time.
  contentTypeMenu(): Locator {
    return this.page.getByTestId("content-type-menu");
  }

  contentTypeOptions(): Locator {
    return this.contentTypeMenu().getByTestId("content-type-option");
  }

  contentTypeOption(_selector: Locator, typeLabel: string): Locator {
    return this.contentTypeOptions()
      .filter({
        has: this.page.getByText(typeLabel, { exact: true }),
      })
      .first();
  }

  async selectContentType(selector: Locator, typeLabel: string): Promise<void> {
    const search = selector.getByRole("textbox", { name: /^Search / });
    await search.fill(typeLabel);
    const option = this.contentTypeOption(selector, typeLabel);
    if ((await option.getAttribute("data-selected")) !== "true") {
      await option.getByRole("button").click();
    }
    await expect(option).toHaveAttribute("data-selected", "true");
    await search.fill("");
    expect(await this.selectedContentTypes(selector)).toContain(typeLabel);
  }

  async removeContentType(selector: Locator, typeLabel: string): Promise<void> {
    await selector
      .getByRole("list", { name: /^Selected / })
      .getByRole("button", { name: `remove ${typeLabel}`, exact: true })
      .click();
    expect(await this.selectedContentTypes(selector)).not.toContain(typeLabel);
  }

  async selectedContentTypes(selector: Locator): Promise<string[]> {
    return (
      await selector.getByTestId("selected-content-type").allInnerTexts()
    ).map((t) => t.trim());
  }
  async restoreContentTypeRemoved(
    selector: Locator,
    typeLabel: string,
  ): Promise<void> {
    if ((await this.selectedContentTypes(selector)).includes(typeLabel)) {
      await this.removeContentType(selector, typeLabel);
      await this.saveIndexConfiguration();
    }
  }
  async rebuildIndex(): Promise<void> {
    await this.indexRow.getByTestId("button-Rebuild").click();
    await expect(this.page.getByText(/Rebuild requested\./)).toBeVisible();
  }
}
