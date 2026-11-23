import { expect, test } from "@playwright/test";
import { LuceneIndexesPage } from "./luceneIndexesPage";
import { openCoffeeSearchWhenReady } from "../shared/search";
import { luceneIndexName } from "./luceneIndexesPage";
import { CreateWizard } from "./createWizard";

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

    await expect(page.getByTestId("config-settings")).toContainText(
      "Display name",
    );
    await expect(page.getByRole("textbox")).toHaveCount(0);
  });

  test("listing shows display name, code name and real entry values", async ({
    page,
  }) => {
    const indexes = new LuceneIndexesPage(page);
    await indexes.goto();
    await indexes.expectIndexListed();

    await expect(
      page.getByRole("columnheader", { name: "Display Name" }),
    ).toBeVisible();
    await expect(
      page.getByRole("columnheader", { name: "Code Name" }),
    ).toBeVisible();
    await expect(
      page.getByRole("columnheader", { name: "Last Updated" }),
    ).toBeVisible();
  });

  test("code name is locked and an untouched form is not changed", async ({
    page,
  }) => {
    const indexes = new LuceneIndexesPage(page);
    await indexes.goto();
    await indexes.expectIndexListed();
    await indexes.openIndex();

    const settings = await indexes.openSectionEditor("Edit index settings");
    await expect(settings).toContainText("Code name (cannot be changed)");
    await expect(settings).toContainText(luceneIndexName);
    await expect(
      settings.getByRole("textbox", { name: "Code name" }),
    ).toHaveCount(0);
    await page.getByRole("button", { name: "Cancel", exact: true }).click();
    await expect(indexes.configPanel()).toHaveCount(0);
    await indexes.goto();
    await indexes.expectIndexListed();
  });

  test("inspector tabs render", async ({ page }) => {
    const indexes = new LuceneIndexesPage(page);
    await indexes.goto();
    await indexes.expectIndexListed();
    await indexes.openInspector("Overview");
    await expect(page.getByTestId("overview-code-name")).toHaveText(
      luceneIndexName,
    );
    await expect(page.getByTestId("status-health")).toBeVisible();

    await indexes.openTab("Configuration");
    await expect(page.getByTestId("config-settings")).toBeVisible();

    await indexes.openTab("Search");
    await page.getByText("Lucene query (advanced)", { exact: true }).click();
    await expect(page.getByLabel("Query", { exact: true })).toBeVisible();
    await expect(page.getByTestId("search-total")).toBeVisible();

    const menu = page.getByTestId("side-navigation-vertical-menu");
    for (const removed of ["Documents", "Test search", "Status"]) {
      await expect(
        menu.getByRole("link", { name: removed, exact: true }),
      ).toHaveCount(0);
    }
  });

  test("search lists stored documents for a blank query", async ({ page }) => {
    const indexes = new LuceneIndexesPage(page);
    await indexes.goto();
    await indexes.expectIndexListed();
    await indexes.openInspector("Search");

    await expect(page.getByTestId("search-total")).toContainText(
      "stored documents",
    );
    await expect(page.getByTestId("search-results")).toBeVisible();
  });

  test("search returns hits with explanation", async ({ page }) => {
    const indexes = new LuceneIndexesPage(page);
    await indexes.goto();
    await indexes.expectIndexListed();
    await indexes.openInspector("Search");

    await page.getByText("Lucene query (advanced)", { exact: true }).click();
    await page.getByLabel("Query", { exact: true }).fill("Title:coffee");
    await page.getByRole("button", { name: "Search", exact: true }).click();
    await expect(page.getByTestId("search-parsed")).toBeVisible();
    await page
      .getByTestId("search-results")
      .getByTestId("table-cell-title")
      .first()
      .click();
    await expect(page.getByTestId("search-explain")).toContainText("weight");
  });

  test("overview reports status without a rebuild note and runs diagnostics", async ({
    page,
  }) => {
    const indexes = new LuceneIndexesPage(page);
    await indexes.goto();
    await indexes.expectIndexListed();
    await indexes.openInspector("Overview");

    await expect(page.getByTestId("status-health")).toBeVisible();
    await expect(page.getByTestId("status-note")).toHaveCount(0);
    await page.getByRole("button", { name: "Run diagnostics" }).click();
    await expect(page.getByTestId("diagnostics-summary")).toBeVisible();
  });

  test("wizard validates, previews pages and advances to review", async ({
    page,
  }, testInfo) => {
    const indexes = new LuceneIndexesPage(page);
    await indexes.openWizard();
    await indexes.expectWizardStep("1. Identity");
    await testInfo.attach("Wizard identity", {
      body: await page.getByTestId("wizard").screenshot({
        path: testInfo.outputPath("wizard-identity.png"),
      }),
      contentType: "image/png",
    });
    await page.getByRole("button", { name: "Next" }).click();
    await expect(page.getByTestId("wizard-error").first()).toContainText(
      "code name",
    );

    await page.getByLabel("Code name").fill(luceneIndexName);
    await page.getByRole("button", { name: "Next", exact: true }).click();
    await expect(page.getByTestId("wizard-error").first()).toContainText(
      "already exists",
    );

    const codeName = `Wizard.Test.${Date.now()}`;
    await page.getByLabel("Code name").fill(codeName);
    await page.getByRole("button", { name: "Next" }).click();
    await expect(page.getByTestId("step-content")).toBeVisible();
    await page.getByRole("button", { name: "Next" }).click();
    await expect(page.getByTestId("wizard-error").first()).toBeVisible();

    const wizard = new CreateWizard(page);
    await wizard.openNewChannel();
    await wizard.fillPath("/%", ["Article page"]);
    await wizard.panelButton("Preview path").click();
    await expect(page.getByTestId("path-preview-summary")).toContainText(
      "Wildcard",
    );
    await expect(page.getByTestId("path-preview-summary")).toContainText(
      /[1-9]\d* matching pages/,
    );
    await expect(
      page.getByTestId("path-preview-item").first(),
    ).toBeInViewport();
    await wizard.closePreview();
    await expect(wizard.pathInput()).toHaveValue("/%");
    await wizard.apply("Add channel");

    await page.getByRole("button", { name: "Next", exact: true }).click();
    await expect(page.getByTestId("step-languages")).toBeVisible();
    await indexes.expectWizardStep("3. Languages and analyzer");
    await page.getByRole("button", { name: "Next", exact: true }).click();
    await expect(page.getByTestId("wizard-error").first()).toContainText(
      "at least one language",
    );
    await indexes.selectWizardCheckbox("Languages");
    await page.getByRole("button", { name: "Next", exact: true }).click();
    await expect(page.getByTestId("step-strategy")).toBeVisible();
    await expect(
      page.getByLabel("Indexing strategy", { exact: true }),
    ).not.toHaveValue("");
    await page.getByRole("button", { name: "Next", exact: true }).click();
    await expect(page.getByTestId("wizard-review")).toContainText(codeName);
    await expect(
      page
        .getByTestId("wizard-review")
        .getByText(codeName, { exact: true })
        .first(),
    ).toBeInViewport();
    await expect(page.getByTestId("wizard-review")).toContainText("/%");
    await indexes.expectWizardStep("5. Review");
    await testInfo.attach("Wizard review", {
      body: await page.getByTestId("wizard").screenshot({
        path: testInfo.outputPath("wizard-review.png"),
      }),
      contentType: "image/png",
    });
    await expect(
      page.getByRole("button", { name: "Create index", exact: true }),
    ).toBeEnabled();
  });

  test("wizard previews the current draft and blocks incomplete previews", async ({
    page,
  }) => {
    const indexes = new LuceneIndexesPage(page);
    await indexes.openWizard();
    await page.getByLabel("Code name").fill(`Wizard.Preview.${Date.now()}`);
    await page.getByRole("button", { name: "Next", exact: true }).click();
    const wizard = new CreateWizard(page);
    await wizard.openNewChannel();
    await wizard.fillPath("/%", ["Article page"]);
    await wizard.panelButton("Preview path").click();
    await expect(page.getByTestId("path-preview-summary")).toContainText(
      "Wildcard",
    );
    await wizard.closePreview();

    // Incomplete drafts cannot be previewed, so no stale result or server error is shown.
    await wizard.pathInput().fill("");
    await expect(wizard.panelButton("Preview path")).toBeDisabled();
    await expect(page.getByTestId("path-preview-summary")).toHaveCount(0);

    await wizard.pathInput().fill(`/wizard-no-page-${Date.now()}`);
    await wizard.panelButton("Preview path").click();
    await expect(page.getByTestId("path-preview-error")).toHaveCount(0);
    await expect(page.getByTestId("path-preview-summary")).toContainText(
      "Exact path",
    );
    await expect(page.getByTestId("path-preview-summary")).not.toContainText(
      "Wildcard",
    );
    await expect(page.getByTestId("path-preview-summary")).toContainText(
      "0 matching pages",
    );
  });

  test("wizard displays creation command errors and allows retry", async ({
    page,
  }) => {
    let result: unknown = {
      success: false,
      indexId: 0,
      errors: ["The selected language is no longer available."],
    };
    await page.route("**/api/cmd", async (route) => {
      const command = /name="command"\r\n\r\n([^\r]+)/.exec(
        route.request().postData() ?? "",
      )?.[1];
      if (command !== "CreateIndex") {
        await route.fallback();
        return;
      }
      await route.fulfill({
        json: { commandName: null, messages: [], result },
      });
    });

    const indexes = new LuceneIndexesPage(page);
    await indexes.openWizard();
    await page.getByLabel("Code name").fill(`Wizard.Errors.${Date.now()}`);
    await page.getByRole("button", { name: "Next", exact: true }).click();
    await new CreateWizard(page).selectReusable(["Coffee product"]);
    await page.getByRole("button", { name: "Next", exact: true }).click();
    await indexes.selectWizardCheckbox("Languages");
    await page.getByRole("button", { name: "Next", exact: true }).click();
    await page.getByRole("button", { name: "Next", exact: true }).click();

    const create = page.getByRole("button", {
      name: "Create index",
      exact: true,
    });
    await create.click();
    await expect(page.getByTestId("wizard-error")).toHaveText(
      "The selected language is no longer available.",
    );
    await expect(create).toBeEnabled();
    await expect(
      page.getByRole("button", { name: "Back", exact: true }),
    ).toBeEnabled();

    result = null;
    await create.click();
    await expect(page.getByTestId("wizard-error")).toHaveText(
      "The index could not be created. Please try again.",
    );
    await expect(create).toBeEnabled();
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
    const originalPaths = await indexes.pathValues();

    const selectedContentType = await indexes.addIncludedPath(originalPath);
    await indexes.saveIndexConfiguration();
    await page.reload();

    await expect(
      indexes
        .configuredChannel()
        .locator(`[data-testid="config-path"][data-path="${originalPath}"]`),
    ).toContainText(selectedContentType);
    await indexes.editIncludedPath(originalPath, editedPath);
    await indexes.saveIndexConfiguration();
    await page.reload();

    expect(await indexes.pathValues()).toContain(editedPath);
    expect(await indexes.pathValues()).not.toContain(originalPath);
    await indexes.removeIncludedPath(editedPath);
    await indexes.saveIndexConfiguration();
    await page.reload();

    await expect(indexes.configuredChannel()).toBeVisible();
    expect(await indexes.pathValues()).toEqual(originalPaths);
  });
});
