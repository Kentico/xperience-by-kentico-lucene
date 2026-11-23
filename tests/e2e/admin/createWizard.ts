import { expect, type Locator, type Page } from "@playwright/test";
import { LuceneIndexesPage } from "./luceneIndexesPage";

// Create index wizard flows: a read-only Content summary edited through one source side panel.
export class CreateWizard {
  readonly indexes: LuceneIndexesPage;

  constructor(private readonly page: Page) {
    this.indexes = new LuceneIndexesPage(page);
  }

  async next(): Promise<void> {
    await this.page.getByRole("button", { name: "Next", exact: true }).click();
  }

  stepButton(name: string): Locator {
    return this.page
      .getByTestId("wizard-steps")
      .getByRole("button", { name, exact: true });
  }

  async start(codeName: string): Promise<void> {
    await this.indexes.openWizard();
    await this.page.getByLabel("Code name").fill(codeName);
    await this.next();
    await expect(this.content()).toBeVisible();
  }

  content(): Locator {
    return this.page.getByTestId("step-content");
  }

  panel(): Locator {
    return this.page.getByTestId("wizard-source-panel");
  }

  form(): Locator {
    return this.page.getByTestId("wizard-source-form");
  }

  pathInput(): Locator {
    return this.form().getByRole("textbox", { name: /Path$/ });
  }

  typesSelector(): Locator {
    return this.indexes.contentTypeSelector(this.form(), "Content types");
  }

  reusableSelector(): Locator {
    return this.indexes.contentTypeSelector(
      this.form(),
      "Reusable content types",
    );
  }

  panelButton(name: string): Locator {
    return this.panel().getByRole("button", { name, exact: true });
  }

  preview(): Locator {
    return this.page.getByTestId("path-preview-body");
  }

  async closePreview(): Promise<void> {
    await this.page
      .getByTestId("path-preview-panel")
      .getByRole("button", { name: "Close", exact: true })
      .last()
      .click();
    await expect(this.preview()).toHaveCount(0);
  }

  /** Opens the source editor for a new channel with the first channel selected, unapplied. */
  async openNewChannel(): Promise<void> {
    await this.indexes.addWizardChannel();
    await expect(this.form()).toBeVisible();
  }

  // Escape on a closed list could reach the side panel, so press it only while the list is open.
  async closeTypeMenu(selector: Locator): Promise<void> {
    if (await this.indexes.contentTypeMenu().isVisible()) {
      await selector.getByRole("textbox", { name: /^Search / }).press("Escape");
    }
    await expect(this.indexes.contentTypeMenu()).toHaveCount(0);
  }

  async fillPath(path: string, types: string[]): Promise<void> {
    await this.pathInput().fill(path);
    for (const type of types) {
      await this.indexes.selectContentType(this.typesSelector(), type);
    }
    if (types.length) {
      await this.closeTypeMenu(this.typesSelector());
    }
  }

  async apply(label: string): Promise<void> {
    await this.panelButton(label).click();
    await expect(this.form()).toHaveCount(0);
  }

  /** Adds the first available channel and returns its display name. */
  async addChannel(path: string, types: string[]): Promise<string> {
    await this.openNewChannel();
    await this.fillPath(path, types);
    await this.apply("Add channel");
    const block = this.channelBlocks().last();
    await expect(block).toHaveAttribute("aria-label", /\S/);
    return (await block.getAttribute("aria-label")) ?? "";
  }

  channelBlocks(): Locator {
    return this.content().getByTestId("channel-block");
  }

  async addPath(channel: string, path: string, types: string[]): Promise<void> {
    await this.content()
      .getByRole("button", { name: `Add path to ${channel}`, exact: true })
      .click();
    await this.fillPath(path, types);
    await this.apply("Add path");
  }

  async selectReusable(types: string[]): Promise<void> {
    await this.content()
      .getByRole("button", { name: /reusable content types$/ })
      .click();
    for (const type of types) {
      await this.indexes.selectContentType(this.reusableSelector(), type);
    }
    await this.closeTypeMenu(this.reusableSelector());
    await this.apply("Apply");
  }

  pathBlocks(): Locator {
    return this.content().getByTestId("path-block");
  }

  async pathValues(): Promise<string[]> {
    return this.content().getByTestId("path-value").allInnerTexts();
  }

  /** Selects the first language and advances to the review step. */
  async toReview(): Promise<void> {
    await this.next();
    await this.indexes.selectWizardCheckbox("Languages");
    await this.next();
    await this.next();
    await this.indexes.expectWizardStep("5. Review");
  }

  review(): Locator {
    return this.page.getByTestId("wizard-review");
  }
}

export const checkboxLabel = async (
  page: Page,
  group: string,
): Promise<string> =>
  (
    await page
      .getByRole("group", { name: group, exact: true })
      .getByRole("checkbox")
      .first()
      .evaluate(
        (el) =>
          (el as HTMLInputElement).labels?.[0]?.textContent ??
          el.getAttribute("aria-label") ??
          "",
      )
  ).trim();

// Unstyled admin text inherits a red default; plain wizard text must not.
export const redTextIn = (scope: Locator): Promise<string[]> =>
  scope.evaluate((root) =>
    [root, ...Array.from(root.querySelectorAll("*"))]
      .filter((el) =>
        Array.from(el.childNodes).some(
          (n) => n.nodeType === Node.TEXT_NODE && n.textContent?.trim(),
        ),
      )
      .filter((el) => {
        const [r, g, b] = (getComputedStyle(el).color.match(/\d+/g) ?? []).map(
          Number,
        );
        return r > 150 && g < 110 && b < 110;
      })
      // Native required-field asterisks are intentionally red.
      .filter((el) => el.textContent.trim() !== "*")
      .map((el) => el.textContent.trim()),
  );
