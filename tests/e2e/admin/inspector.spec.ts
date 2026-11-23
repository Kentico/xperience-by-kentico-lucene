import { expect, test, type Page, type Request } from "@playwright/test";
import { luceneIndexName } from "./luceneIndexesPage";
import { adminBaseUrl } from "../shared/config";

const status = {
  hasPublishedGeneration: true,
  publishedGeneration: 2,
  generations: [{ generation: 2, isPublished: true }],
  documentCount: 41,
  deletedDocumentCount: 0,
  lastUpdatedUtc: "2026-11-22T12:00:00Z",
  lastRebuildRequest: null,
  readError: null,
};
const summary = {
  indexName: luceneIndexName,
  displayName: luceneIndexName,
  strategyName: "DancingGoatGlobalSearch",
  analyzerName: "Standard",
  languages: ["en", "es"],
  reusableContentTypes: ["DancingGoat.Coffee"],
  channels: [
    {
      channelName: "Dancing Goat Pages",
      paths: [{ aliasPath: "/%", contentTypes: ["DancingGoat.Article"] }],
    },
  ],
  status,
};
const document = {
  documentId: 7,
  fields: [
    { name: "ItemGuid", value: "inspector-regression-document" },
    { name: "LanguageName", value: "en" },
    { name: "Title", value: "Coffee from the published index" },
  ],
};
const documents = { total: 1, documents: [document], readError: null };
const queryResult = {
  parsedQuery: "Title:coffee",
  totalHits: 1,
  hits: [
    {
      document,
      score: 0.75,
      explanation: "0.75 = weight(Title:coffee in 7)",
    },
  ],
  error: null,
};
const diagnosticItem = {
  itemGuid: "inspector-skipped-item",
  languageName: "es",
  contentTypeName: "DancingGoat.Article",
  name: "Skipped article",
  reason: "Excluded by the indexing strategy",
};
const diagnostics = {
  selectedItemCount: 2,
  indexableCount: 1,
  skippedCount: 1,
  missingCount: 0,
  orphanCount: 0,
  skipped: [diagnosticItem],
  missing: [],
  orphans: [],
  error: null,
};
// The diagnostics run keeps no server-side snapshot, so the first page is all there is.
const diagnosticsRun = { runId: null, diagnostics };

const commandName = (request: Request): string | undefined =>
  /name="command"\r\n\r\n([^\r]+)/.exec(request.postData() ?? "")?.[1];

async function mockCommand(
  page: Page,
  command: string,
  result: unknown,
): Promise<void> {
  await page.route("**/api/cmd", async (route) => {
    if (commandName(route.request()) !== command) {
      await route.fallback();
      return;
    }
    await route.fulfill({
      json: { commandName: null, messages: [], result },
    });
  });
}

class InspectorPage {
  constructor(private readonly page: Page) {}

  async openTab(tab: string): Promise<void> {
    await this.page
      .getByTestId("side-navigation-vertical-menu")
      .getByRole("link", { name: tab, exact: true })
      .click();
  }
}

async function openInspector(page: Page, tab: string): Promise<InspectorPage> {
  const indexes = new InspectorPage(page);
  await page.goto(`${adminBaseUrl}/lucene/indexes`);
  const row = page.getByRole("row").filter({
    has: page.getByRole("cell", { name: luceneIndexName, exact: true }),
  });
  await expect(row).toBeVisible();
  await row.getByRole("link").first().click();
  // The index link lands on Overview; clicking it again remounts the tab and repeats its command.
  if (tab === "Overview") {
    await expect(page.getByTestId("inspector-overview")).toBeVisible();
  } else {
    await indexes.openTab(tab);
  }
  return indexes;
}

async function useAdvancedQuery(page: Page): Promise<void> {
  // The native radio input sits under its decorative circle, so select it through its visible label.
  await page.getByText("Lucene query (advanced)", { exact: true }).click();
  await expect(
    page.getByRole("radio", { name: "Lucene query (advanced)", exact: true }),
  ).toBeChecked();
}

async function submitCommand(page: Page, command: string): Promise<void> {
  if (command === "RunQuery") {
    await useAdvancedQuery(page);
    await page.getByLabel("Query", { exact: true }).fill("Title:coffee");
    await page.getByRole("button", { name: "Search", exact: true }).click();
  } else if (command === "StartDiagnostics") {
    await page
      .getByRole("button", { name: "Run diagnostics", exact: true })
      .click();
  }
}

test.describe("Lucene inspector command regressions", () => {
  test("renders the live successful command envelope instead of staying loading", async ({
    page,
  }, testInfo) => {
    const responsePromise = page.waitForResponse(
      (response) => commandName(response.request()) === "LoadSummary",
    );
    await openInspector(page, "Overview");
    const response = await responsePromise;
    expect(response.ok()).toBe(true);
    const envelope = (await response.json()) as {
      result: { indexName: string; displayName: string; analyzerName: string };
    };
    expect(envelope.result.indexName).toBe(luceneIndexName);
    await expect(page.getByTestId("overview-code-name")).toHaveText(
      envelope.result.indexName,
    );
    await expect(page.getByTestId("overview-display-name")).toHaveText(
      envelope.result.displayName,
    );
    await expect(page.getByText("Analyzer", { exact: true })).toBeVisible();
    await expect(
      page
        .getByTestId("inspector-overview")
        .getByText(envelope.result.analyzerName, { exact: true }),
    ).toBeVisible();
    await expect(page.getByTestId("inspector-loading")).toHaveCount(0);
    await expect(page.getByTestId("inspector-error")).toHaveCount(0);
    const screenshotPath = testInfo.outputPath("inspector-overview.png");
    await page.screenshot({ path: screenshotPath, fullPage: true });
    await testInfo.attach("inspector-overview", {
      path: screenshotPath,
      contentType: "image/png",
    });
  });

  test("renders envelope results for every tab, stored fields, query explanations and diagnostics", async ({
    page,
  }) => {
    await mockCommand(page, "LoadSummary", summary);
    await mockCommand(page, "LoadDocuments", documents);
    await mockCommand(page, "RunQuery", queryResult);
    await mockCommand(page, "StartDiagnostics", diagnosticsRun);
    const indexes = await openInspector(page, "Overview");
    await expect(page.getByTestId("overview-code-name")).toHaveText(
      luceneIndexName,
    );
    await expect(page.getByText("Languages", { exact: true })).toBeVisible();
    await expect(
      page
        .getByTestId("inspector-overview")
        .getByText("en, es", { exact: true }),
    ).toBeVisible();
    await expect(page.getByTestId("overview-channels")).toContainText(
      "Dancing Goat Pages",
    );

    await expect(page.getByTestId("status-documents")).toHaveText("41");

    await indexes.openTab("Search");
    await expect(page.getByTestId("search-total")).toHaveText(
      "Showing 1–1 of 1 stored document.",
    );
    await expect(page.getByTestId("search-pages")).toHaveCount(0);
    await page.getByTestId("table-cell-title").first().click();
    await expect(page.getByTestId("search-panel-fields")).toContainText(
      "inspector-regression-document",
    );
    await page.getByTestId("side-panel-close").click();

    await useAdvancedQuery(page);
    await page.getByLabel("Query", { exact: true }).fill("Title:coffee");
    await page.getByRole("button", { name: "Search", exact: true }).click();
    await expect(page.getByTestId("search-total")).toHaveText(
      "Showing 1–1 of 1 matching document, highest score first.",
    );
    await page.getByTestId("table-cell-title").first().click();
    await expect(page.getByTestId("search-explain")).toHaveText(
      queryResult.hits[0].explanation,
    );
    await page.getByTestId("side-panel-close").click();

    await indexes.openTab("Overview");
    await page
      .getByRole("button", { name: "Run diagnostics", exact: true })
      .click();
    await expect(page.getByTestId("diagnostics-summary")).toContainText(
      "2 items selected",
    );
    await page
      .getByTestId("diagnostics-skipped")
      .getByRole("button", { name: /Skipped by the strategy/ })
      .click();
    await expect(page.getByTestId("diagnostics-skipped")).toContainText(
      diagnosticItem.reason,
    );
    await expect(page.getByTestId("inspector-loading")).toHaveCount(0);
    await expect(page.getByTestId("inspector-error")).toHaveCount(0);
  });

  test("reads live stored documents and runs a real query and diagnostics", async ({
    page,
  }, testInfo) => {
    const indexes = await openInspector(page, "Search");
    await expect(page.getByTestId("search-total")).toContainText(
      "stored documents",
    );
    await page.getByTestId("table-cell-title").first().click();
    await expect(page.getByTestId("search-panel-fields")).toContainText(
      "ItemGuid",
    );
    await page.getByTestId("side-panel-close").click();

    await useAdvancedQuery(page);
    await page.getByLabel("Query", { exact: true }).fill("*:*");
    await page.getByRole("button", { name: "Search", exact: true }).click();
    await expect(page.getByTestId("search-total")).toContainText(
      "matching documents",
    );
    await page.getByTestId("table-cell-title").first().click();
    await expect(page.getByTestId("search-explain")).not.toBeEmpty();
    await page.screenshot({
      path: testInfo.outputPath("inspector-query.png"),
      fullPage: true,
    });
    await page.getByTestId("side-panel-close").click();

    await indexes.openTab("Overview");
    await expect(page.getByTestId("status-table")).toBeVisible();
    await page
      .getByRole("button", { name: "Run diagnostics", exact: true })
      .click();
    await expect(page.getByTestId("diagnostics-summary")).toContainText(
      "items selected",
    );
    await expect(page.getByTestId("diagnostics-skipped")).toBeVisible();
    await expect(page.getByTestId("inspector-error")).toHaveCount(0);
    await expect(page.getByTestId("inspector-loading")).toHaveCount(0);
    await page.screenshot({
      path: testInfo.outputPath("inspector-status.png"),
      fullPage: true,
    });
  });

  for (const scenario of [
    {
      tab: "Overview",
      command: "LoadSummary",
      result: summary,
      ready: "overview-code-name",
    },
    {
      tab: "Search",
      command: "LoadDocuments",
      result: documents,
      ready: "search-total",
    },
    {
      tab: "Search",
      command: "RunQuery",
      result: queryResult,
      ready: "search-results",
      button: "Search",
    },
    {
      tab: "Overview",
      command: "StartDiagnostics",
      result: diagnosticsRun,
      ready: "diagnostics-result",
      button: "Run diagnostics",
    },
  ]) {
    test(`${scenario.tab} ${scenario.command} reports a missing command result and can recover`, async ({
      page,
    }) => {
      await mockCommand(page, scenario.command, null);
      await openInspector(page, scenario.tab);
      await submitCommand(page, scenario.command);
      await expect(page.getByTestId("inspector-error")).toContainText(
        "The server did not return a result",
      );
      await expect(page.getByTestId("inspector-loading")).toHaveCount(0);

      await mockCommand(page, scenario.command, scenario.result);
      await page
        .getByRole("button", { name: scenario.button ?? "Retry", exact: true })
        .click();
      await expect(page.getByTestId(scenario.ready)).toBeVisible();
      await expect(page.getByTestId("inspector-error")).toHaveCount(0);
    });
  }

  test("shows payload read and query errors without treating them as loading", async ({
    page,
  }) => {
    await mockCommand(page, "LoadSummary", {
      ...summary,
      status: { ...status, readError: "The published index cannot be read." },
    });
    const indexes = await openInspector(page, "Overview");
    await expect(page.getByTestId("inspector-error")).toContainText(
      "The published index cannot be read.",
    );
    await expect(page.getByTestId("overview-code-name")).toHaveText(
      luceneIndexName,
    );

    await mockCommand(page, "LoadDocuments", {
      total: 0,
      documents: [],
      readError: "Stored documents cannot be read.",
    });
    await indexes.openTab("Search");
    await expect(page.getByTestId("inspector-error")).toContainText(
      "Stored documents cannot be read.",
    );
    await expect(page.getByTestId("inspector-loading")).toHaveCount(0);

    await mockCommand(page, "RunQuery", {
      ...queryResult,
      hits: [],
      error: "Invalid Lucene query syntax.",
    });
    await useAdvancedQuery(page);
    await page.getByLabel("Query", { exact: true }).fill("Title:(");
    await page.getByRole("button", { name: "Search", exact: true }).click();
    await expect(page.getByTestId("inspector-error")).toContainText(
      "Invalid Lucene query syntax.",
    );
    await expect(
      page.getByRole("button", { name: "Search", exact: true }),
    ).toBeEnabled();
    await expect(page.getByTestId("search-results")).toHaveCount(0);

    await mockCommand(page, "StartDiagnostics", {
      runId: null,
      diagnostics: {
        ...diagnostics,
        error: "Diagnostics cannot read the index.",
      },
    });
    await indexes.openTab("Overview");
    await page
      .getByRole("button", { name: "Run diagnostics", exact: true })
      .click();
    await expect(page.getByTestId("inspector-error").last()).toContainText(
      "Diagnostics cannot read the index.",
    );
    await expect(
      page.getByRole("button", { name: "Run diagnostics", exact: true }),
    ).toBeEnabled();
  });

  test("network failures end loading and allow retry", async ({ page }) => {
    await page.route("**/api/cmd", async (route) => {
      if (commandName(route.request()) === "LoadDocuments") {
        await route.abort("failed");
      } else {
        await route.fallback();
      }
    });
    await openInspector(page, "Search");
    await expect(page.getByTestId("inspector-error")).toBeVisible();
    await expect(page.getByTestId("inspector-loading")).toHaveCount(0);
    await mockCommand(page, "LoadDocuments", documents);
    await page.getByRole("button", { name: "Retry", exact: true }).click();
    await expect(page.getByTestId("search-total")).toBeVisible();
    await expect(page.getByTestId("inspector-error")).toHaveCount(0);
  });

  test("a stalled command times out, releases its busy state and can be retried", async ({
    page,
  }) => {
    await openInspector(page, "Search");
    await expect(page.getByTestId("search-total")).toBeVisible();
    await page.clock.install();
    await page.route("**/api/cmd", async (route) => {
      if (commandName(route.request()) !== "RunQuery") {
        await route.fallback();
      }
    });
    const requestPromise = page.waitForRequest(
      (request) => commandName(request) === "RunQuery",
    );
    await submitCommand(page, "RunQuery");
    await requestPromise;
    await expect(page.getByTestId("inspector-loading")).toBeVisible();
    await expect(
      page.getByRole("button", { name: "Search", exact: true }),
    ).toBeDisabled();
    await page.clock.fastForward(60_001);
    await expect(page.getByTestId("inspector-error")).toContainText(
      "The request timed out",
    );
    await expect(page.getByTestId("inspector-loading")).toHaveCount(0);
    await expect(
      page.getByRole("button", { name: "Search", exact: true }),
    ).toBeEnabled();

    await mockCommand(page, "RunQuery", queryResult);
    await page.getByRole("button", { name: "Search", exact: true }).click();
    await expect(page.getByTestId("search-results")).toBeVisible();
    await expect(page.getByTestId("inspector-error")).toHaveCount(0);
  });
});
