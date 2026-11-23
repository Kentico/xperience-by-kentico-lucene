import { expect, test, type Page, type Request } from "@playwright/test";
import { luceneIndexName } from "./luceneIndexesPage";
import { adminBaseUrl } from "../shared/config";

interface StatusFixture {
  hasPublishedGeneration: boolean;
  publishedGeneration: number | null;
  generations: { generation: number; isPublished: boolean }[];
  documentCount: number;
  deletedDocumentCount: number;
  lastUpdatedUtc: string | null;
  lastRebuildRequest: {
    requestedAtUtc: string;
    queuedItemCount: number;
  } | null;
  readError: string | null;
}

type Category = "skipped" | "missing" | "orphans";

interface DiagnosticItem {
  itemGuid: string;
  languageName: string;
  contentTypeName: string;
  name: string;
  reason: string;
}

interface PageArguments {
  runId: string;
  category: Category;
  skip: number;
  take: number;
}

const status: StatusFixture = {
  hasPublishedGeneration: true,
  publishedGeneration: 3,
  generations: [
    { generation: 2, isPublished: false },
    { generation: 3, isPublished: true },
  ],
  documentCount: 41,
  deletedDocumentCount: 5,
  lastUpdatedUtc: "2026-11-22T12:00:00Z",
  lastRebuildRequest: {
    requestedAtUtc: "2026-11-22T11:00:00Z",
    queuedItemCount: 46,
  },
  readError: null,
};
const summary = (statusOverride: Partial<StatusFixture> = {}) => ({
  indexName: luceneIndexName,
  displayName: luceneIndexName,
  strategyName: "DancingGoatGlobalSearch",
  analyzerName: "Standard",
  languages: ["en"],
  reusableContentTypes: [],
  channels: [],
  status: { ...status, ...statusOverride },
});

const PAGE_SIZE = 20;
const reasons: Record<Category, string> = {
  skipped: "Secured content is excluded.",
  missing: "Indexable but not found.",
  orphans: "Not selected by the configuration.",
};
const totals: Record<Category, number> = {
  skipped: 45,
  missing: 201,
  orphans: 21,
};
const label = (category: Category, index: number): string =>
  `${category} item ${String(index + 1)}`;
// Generates a category page on demand, so large totals need no stored records.
const items = (
  category: Category,
  skip: number,
  take: number,
): DiagnosticItem[] =>
  Array.from(
    { length: Math.max(0, Math.min(take, totals[category] - skip)) },
    (_, offset) => ({
      itemGuid: `${category}-${String(skip + offset)}-guid`,
      languageName: "en",
      contentTypeName: "DancingGoat.Article",
      name: label(category, skip + offset),
      reason: reasons[category],
    }),
  );
const diagnostics = (error: string | null = null) => ({
  selectedItemCount: 250,
  indexableCount: 205,
  skippedCount: totals.skipped,
  missingCount: totals.missing,
  orphanCount: totals.orphans,
  skipped: items("skipped", 0, PAGE_SIZE),
  missing: items("missing", 0, PAGE_SIZE),
  orphans: items("orphans", 0, PAGE_SIZE),
  error,
});

const formField = (request: Request, name: string): string | undefined =>
  new RegExp(`name="${name}"\\r\\n\\r\\n([^\\r]+)`).exec(
    request.postData() ?? "",
  )?.[1];
const commandName = (request: Request): string | undefined =>
  formField(request, "command");
const pageArguments = (request: Request): PageArguments =>
  JSON.parse(formField(request, "data") ?? "{}") as PageArguments;

async function mockCommand(
  page: Page,
  command: string,
  result: unknown,
): Promise<void> {
  await respondToCommand(page, command, () => result);
}

async function respondToCommand(
  page: Page,
  command: string,
  respond: (request: Request) => unknown,
): Promise<void> {
  await page.route("**/api/cmd", async (route) => {
    const request = route.request();
    if (commandName(request) !== command) {
      await route.fallback();
      return;
    }
    await route.fulfill({
      json: {
        commandName: null,
        messages: [],
        result: respond(request),
      },
    });
  });
}

async function mockDiagnostics(page: Page, runId = "run-1"): Promise<void> {
  await mockCommand(page, "StartDiagnostics", {
    runId,
    diagnostics: diagnostics(),
  });
  await respondToCommand(page, "LoadDiagnosticsPage", (request) => {
    const args = pageArguments(request);
    return {
      total: totals[args.category],
      items: items(args.category, args.skip, args.take),
      error: null,
    };
  });
}

async function openOverviewHealth(page: Page): Promise<void> {
  await page.goto(`${adminBaseUrl}/lucene/indexes`);
  const row = page.getByRole("row").filter({
    has: page.getByRole("cell", { name: luceneIndexName, exact: true }),
  });
  await row.getByRole("link").first().click();
  await page
    .getByTestId("side-navigation-vertical-menu")
    .getByRole("link", { name: "Overview", exact: true })
    .click();
  await expect(page.getByTestId("status-table")).toBeVisible();
}

async function runDiagnostics(page: Page): Promise<void> {
  await page
    .getByRole("button", { name: "Run diagnostics", exact: true })
    .click();
  await expect(page.getByTestId("diagnostics-result")).toBeVisible();
}

const header = (page: Page, testId: string, title: string) =>
  page.getByTestId(testId).getByRole("button", { name: new RegExp(title) });
const pager = (page: Page, testId: string) =>
  page.getByTestId(`${testId}-pages`);
const pagerButton = (page: Page, testId: string, name: string) =>
  pager(page, testId).getByRole("button", { name, exact: true });
async function showPage(
  page: Page,
  testId: string,
  name: string,
): Promise<void> {
  const button = pagerButton(page, testId, name);
  await button.click();
}
const range = (page: Page, testId: string) =>
  page.getByTestId(`${testId}-range`);

test.describe("Lucene Overview index health and diagnostics", () => {
  test("leads with searchability and labels generations, deletes and the server-local rebuild request", async ({
    page,
  }) => {
    await mockCommand(page, "LoadSummary", summary());
    await openOverviewHealth(page);
    await expect(page.getByTestId("status-health")).toContainText(
      "Searchable: generation 3 is published",
    );
    await expect(page.getByTestId("status-documents")).toHaveText("41");
    await expect(page.getByTestId("status-deleted")).toHaveText("5");
    await expect(page.getByTestId("status-generations")).toHaveText(
      "2 (not searched), 3 (published, searched)",
    );
    // The request date and queued count are separate rows, so neither value wraps and clips.
    await expect(page.getByTestId("status-rebuild")).not.toContainText(
      "queued",
    );
    await expect(page.getByTestId("status-rebuild")).not.toHaveText(
      "None since this server started",
    );
    await expect(page.getByTestId("status-rebuild-queued")).toHaveText("46");

    // Explanations are row tooltips, not a separate help section.
    await expect(page.getByTestId("status-notes")).toHaveCount(0);
    await expect(page.getByTestId("status-note")).toHaveCount(0);
    await expect(page.getByText("About these values")).toHaveCount(0);
    const help = page.getByTestId("status-help");
    await expect(help).toHaveCount(7);
    const rebuildHelp = page
      .getByTestId("status-table")
      .getByRole("row")
      .filter({ hasText: "Last rebuild request" })
      .getByTestId("status-help");
    await expect(rebuildHelp).toHaveAttribute(
      "aria-label",
      /in memory on this server only/,
    );
    await expect(
      page
        .getByTestId("status-table")
        .getByRole("row")
        .filter({ hasText: "Items queued" })
        .getByTestId("status-help"),
    ).toHaveAttribute("aria-label", /Processing is not tracked/);
    await expect(
      page
        .getByTestId("status-table")
        .getByRole("row")
        .filter({ hasText: "Generations on disk" })
        .getByTestId("status-help"),
    ).toHaveAttribute(
      "aria-label",
      /Only the published generation is searched/,
    );

    await expect(
      page
        .getByTestId("side-navigation-vertical-menu")
        .getByRole("link", { name: "Status", exact: true }),
    ).toHaveCount(0);

    const refresh = page.waitForRequest(
      (request) => commandName(request) === "LoadSummary",
    );
    await page
      .getByRole("button", { name: "Refresh status", exact: true })
      .click();
    await refresh;
    await expect(page.getByTestId("status-documents")).toHaveText("41");
  });

  test("row help opens a native tooltip on keyboard focus and on hover", async ({
    page,
  }) => {
    await mockCommand(page, "LoadSummary", summary());
    await openOverviewHealth(page);
    const row = (name: string) =>
      page
        .getByTestId("status-table")
        .getByRole("row")
        .filter({ hasText: name })
        .getByTestId("status-help");

    const storedText = "Live documents that searches can return.";
    await expect(page.getByText(storedText)).toHaveCount(0);
    await row("Stored documents").focus();
    await expect(row("Stored documents")).toBeFocused();
    await expect(page.getByText(storedText)).toBeVisible();
    await page.keyboard.press("Tab");
    await expect(page.getByText(storedText)).toHaveCount(0);

    await row("Deleted documents").hover();
    await expect(
      page.getByText(/removed when Lucene merges segments/),
    ).toBeVisible();
  });

  test("does not report counts for an unpublished or unreadable index", async ({
    page,
  }) => {
    await mockCommand(
      page,
      "LoadSummary",
      summary({
        hasPublishedGeneration: false,
        publishedGeneration: null,
        generations: [],
        documentCount: 0,
        deletedDocumentCount: 0,
        lastUpdatedUtc: null,
        lastRebuildRequest: null,
      }),
    );
    await openOverviewHealth(page);
    await expect(page.getByTestId("status-health")).toContainText(
      "Not searchable",
    );
    await expect(page.getByTestId("status-published")).toHaveText("None");
    await expect(page.getByTestId("status-documents")).toHaveText(
      "Unavailable",
    );
    await expect(page.getByTestId("status-rebuild")).toHaveText(
      "None since this server started",
    );
    await expect(page.getByTestId("status-rebuild-queued")).toHaveCount(0);
    await expect(page.getByTestId("status-help")).toHaveCount(6);

    await page.unrouteAll({ behavior: "wait" });
    await mockCommand(
      page,
      "LoadSummary",
      summary({ readError: "The published index cannot be read." }),
    );
    await page
      .getByRole("button", { name: "Refresh status", exact: true })
      .click();
    await expect(page.getByTestId("status-health")).toContainText(
      "is published but cannot be read",
    );
    await expect(page.getByTestId("inspector-error")).toContainText(
      "The published index cannot be read.",
    );
    await expect(page.getByTestId("status-documents")).toHaveText(
      "Unavailable",
    );
  });

  test("diagnostic categories are native full-row toggles with expanded state and keyboard access", async ({
    page,
  }, testInfo) => {
    await mockCommand(page, "LoadSummary", summary());
    await mockDiagnostics(page);
    await openOverviewHealth(page);
    await runDiagnostics(page);
    await expect(page.getByTestId("diagnostics-health")).toContainText(
      "201 indexable items are missing and 21 stored documents are no longer selected",
    );
    await expect(page.getByTestId("diagnostics-summary")).toHaveText(
      "250 items selected, 205 indexable, 45 skipped, 201 not in the index, 21 stored but not selected.",
    );
    await expect(page.getByTestId("diagnostics-ran-at")).toContainText(
      "Last run",
    );

    const missing = header(
      page,
      "diagnostics-missing",
      "missing from the index",
    );
    await expect(missing).toHaveAttribute("aria-expanded", "true");
    await expect(page.getByTestId("diagnostics-missing-rows")).toContainText(
      "missing item 1",
    );
    await expect(range(page, "diagnostics-missing")).toHaveText(
      "Showing 1–20 of 201 items.",
    );

    const skipped = header(
      page,
      "diagnostics-skipped",
      "Skipped by the strategy",
    );
    await expect(skipped).toHaveAttribute("aria-expanded", "false");
    await expect(page.getByTestId("diagnostics-skipped-rows")).toHaveCount(0);
    const box = await skipped.boundingBox();
    expect(box?.height).toBeGreaterThanOrEqual(32);
    await skipped.click({ position: { x: 20, y: (box?.height ?? 40) / 2 } });
    await expect(skipped).toHaveAttribute("aria-expanded", "true");
    await expect(page.getByTestId("diagnostics-skipped-rows")).toContainText(
      "Secured content is excluded.",
    );

    await skipped.focus();
    await page.keyboard.press("Space");
    await expect(skipped).toHaveAttribute("aria-expanded", "false");
    await page.keyboard.press("Enter");
    await expect(skipped).toHaveAttribute("aria-expanded", "true");
    await page.setViewportSize({ width: 1280, height: 2400 });
    await page.screenshot({
      path: testInfo.outputPath("status-diagnostics.png"),
      fullPage: true,
    });
  });

  test("an empty category is a plain row without a toggle or pages", async ({
    page,
  }) => {
    await mockCommand(page, "LoadSummary", summary());
    await mockCommand(page, "StartDiagnostics", {
      runId: "run-empty",
      diagnostics: {
        ...diagnostics(),
        missingCount: 0,
        orphanCount: 0,
        missing: [],
        orphans: [],
      },
    });
    await openOverviewHealth(page);
    await runDiagnostics(page);
    await expect(page.getByTestId("diagnostics-health")).toContainText(
      "Every indexable item is stored",
    );
    for (const testId of ["diagnostics-missing", "diagnostics-orphans"]) {
      await expect(page.getByTestId(testId).getByRole("button")).toHaveCount(0);
      await expect(page.getByTestId(`${testId}-rows`)).toHaveCount(0);
      await expect(pager(page, testId)).toHaveCount(0);
    }
    await expect(page.getByTestId("diagnostics-orphans")).toContainText(
      "Stored but not selected (0)",
    );
  });

  test("each category pages every result from the server independently", async ({
    page,
  }, testInfo) => {
    await mockCommand(page, "LoadSummary", summary());
    await mockDiagnostics(page);
    const requests: PageArguments[] = [];
    page.on("request", (request) => {
      if (commandName(request) === "LoadDiagnosticsPage") {
        requests.push(pageArguments(request));
      }
    });
    await openOverviewHealth(page);
    await runDiagnostics(page);

    // Missing: 201 items, so the last page has a single row.
    const missingRows = page.getByTestId("diagnostics-missing-rows");
    await expect(pager(page, "diagnostics-missing")).toHaveAttribute(
      "aria-label",
      "Indexable but missing from the index pages",
    );
    await expect(pagerButton(page, "diagnostics-missing", "11")).toBeVisible();
    await expect(pagerButton(page, "diagnostics-missing", "12")).toHaveCount(0);
    await showPage(page, "diagnostics-missing", "2");
    await expect(range(page, "diagnostics-missing")).toHaveText(
      "Showing 21–40 of 201 items.",
    );
    await expect(missingRows).toContainText("missing item 21");
    await expect(
      missingRows.getByRole("cell", { name: "missing item 1", exact: true }),
    ).toHaveCount(0);
    await showPage(page, "diagnostics-missing", "11");
    await expect(range(page, "diagnostics-missing")).toHaveText(
      "Showing 201–201 of 201 items.",
    );
    await expect(missingRows).toContainText("missing item 201");
    await expect(
      missingRows.getByRole("cell", { name: "missing item 200", exact: true }),
    ).toHaveCount(0);

    // Orphans keep their own page while missing stays on its last page.
    await showPage(page, "diagnostics-orphans", "2");
    await expect(range(page, "diagnostics-orphans")).toHaveText(
      "Showing 21–21 of 21 items.",
    );
    await expect(page.getByTestId("diagnostics-orphans-rows")).toContainText(
      "orphans item 21",
    );
    await expect(range(page, "diagnostics-missing")).toHaveText(
      "Showing 201–201 of 201 items.",
    );

    // Skipped opens on its first page from the run, then pages on demand.
    await header(
      page,
      "diagnostics-skipped",
      "Skipped by the strategy",
    ).click();
    await expect(range(page, "diagnostics-skipped")).toHaveText(
      "Showing 1–20 of 45 items.",
    );
    await showPage(page, "diagnostics-skipped", "3");
    await expect(range(page, "diagnostics-skipped")).toHaveText(
      "Showing 41–45 of 45 items.",
    );
    // Page 1 is never refetched; every other page asks for one 20-row page of this run.
    await showPage(page, "diagnostics-skipped", "1");
    await expect(range(page, "diagnostics-skipped")).toHaveText(
      "Showing 1–20 of 45 items.",
    );
    expect(requests).toEqual([
      { runId: "run-1", category: "missing", skip: 20, take: 20 },
      { runId: "run-1", category: "missing", skip: 200, take: 20 },
      { runId: "run-1", category: "orphans", skip: 20, take: 20 },
      { runId: "run-1", category: "skipped", skip: 40, take: 20 },
    ]);
    await page.setViewportSize({ width: 1280, height: 2600 });
    await page.screenshot({
      path: testInfo.outputPath("status-diagnostics-paging.png"),
      fullPage: true,
    });
  });

  test("running diagnostics again resets every category to its first page", async ({
    page,
  }) => {
    await mockCommand(page, "LoadSummary", summary());
    await mockDiagnostics(page);
    await openOverviewHealth(page);
    await runDiagnostics(page);
    await showPage(page, "diagnostics-missing", "3");
    await expect(range(page, "diagnostics-missing")).toHaveText(
      "Showing 41–60 of 201 items.",
    );

    await page.unrouteAll({ behavior: "wait" });
    await mockCommand(page, "LoadSummary", summary());
    await mockDiagnostics(page, "run-2");
    const pageRequest = page.waitForRequest(
      (request) => commandName(request) === "LoadDiagnosticsPage",
    );
    await runDiagnostics(page);
    await expect(range(page, "diagnostics-missing")).toHaveText(
      "Showing 1–20 of 201 items.",
    );
    await expect(page.getByTestId("diagnostics-missing-rows")).toContainText(
      "missing item 1",
    );
    // The next page belongs to the new run.
    await showPage(page, "diagnostics-missing", "2");
    expect(pageArguments(await pageRequest)).toMatchObject({
      runId: "run-2",
      skip: 20,
    });
  });

  test("an expired run asks to run again and a failed page request can be retried", async ({
    page,
  }) => {
    await mockCommand(page, "LoadSummary", summary());
    await mockCommand(page, "StartDiagnostics", {
      runId: "run-1",
      diagnostics: diagnostics(),
    });
    const expired =
      "These diagnostic results have expired. Run diagnostics again.";
    await mockCommand(page, "LoadDiagnosticsPage", {
      total: 0,
      items: [],
      error: expired,
    });
    await openOverviewHealth(page);
    await runDiagnostics(page);
    const missing = page.getByTestId("diagnostics-missing-rows");
    await showPage(page, "diagnostics-missing", "2");
    await expect(missing.getByTestId("inspector-error")).toContainText(expired);
    // Retrying the same expired run cannot help, so only running again is offered.
    await expect(missing.getByRole("button", { name: "Retry" })).toHaveCount(0);
    await expect(range(page, "diagnostics-missing")).toHaveCount(0);

    await page.unrouteAll({ behavior: "wait" });
    await mockCommand(page, "LoadSummary", summary());
    await mockCommand(page, "StartDiagnostics", {
      runId: "run-1",
      diagnostics: diagnostics(),
    });
    await page.route("**/api/cmd", async (route) => {
      if (commandName(route.request()) !== "LoadDiagnosticsPage") {
        await route.fallback();
        return;
      }
      await route.abort();
    });
    await showPage(page, "diagnostics-missing", "3");
    await expect(missing.getByTestId("inspector-error")).toBeVisible();
    await expect(range(page, "diagnostics-missing")).toHaveCount(0);

    await page.unrouteAll({ behavior: "wait" });
    await mockCommand(page, "LoadSummary", summary());
    await mockDiagnostics(page);
    await missing.getByRole("button", { name: "Retry", exact: true }).click();
    await expect(missing.getByTestId("inspector-error")).toHaveCount(0);
    await expect(range(page, "diagnostics-missing")).toHaveText(
      "Showing 41–60 of 201 items.",
    );
  });

  test("diagnostics errors offer a retry that recovers", async ({ page }) => {
    await mockCommand(page, "LoadSummary", summary());
    await mockCommand(page, "StartDiagnostics", {
      runId: null,
      diagnostics: diagnostics("Diagnostics cannot read the index."),
    });
    await openOverviewHealth(page);
    await page
      .getByRole("button", { name: "Run diagnostics", exact: true })
      .click();
    await expect(page.getByTestId("inspector-error")).toContainText(
      "Diagnostics cannot read the index.",
    );
    await expect(page.getByTestId("diagnostics-result")).toHaveCount(0);

    await page.unrouteAll({ behavior: "wait" });
    await mockCommand(page, "LoadSummary", summary());
    await mockDiagnostics(page);
    await page.getByRole("button", { name: "Retry", exact: true }).click();
    await expect(page.getByTestId("diagnostics-result")).toBeVisible();
    await expect(page.getByTestId("inspector-error")).toHaveCount(0);
  });

  test("live status and diagnostics render real values with accurate pages", async ({
    page,
  }, testInfo) => {
    const started = page.waitForResponse(
      (response) => commandName(response.request()) === "StartDiagnostics",
    );
    await openOverviewHealth(page);
    await expect(page.getByTestId("status-health")).toContainText(
      "Searchable: generation",
    );
    await expect(page.getByTestId("status-documents")).toHaveText(/^\d+$/);
    await runDiagnostics(page);
    const { result } = (await (await started).json()) as {
      result: {
        runId: string;
        diagnostics: {
          selectedItemCount: number;
          indexableCount: number;
          skippedCount: number;
          missingCount: number;
          orphanCount: number;
        } & Record<Category, DiagnosticItem[]>;
      };
    };
    const run = result.diagnostics;
    expect(result.runId).toBeTruthy();
    await expect(page.getByTestId("diagnostics-summary")).toHaveText(
      `${String(run.selectedItemCount)} items selected, ${String(run.indexableCount)} indexable, ${String(run.skippedCount)} skipped, ${String(run.missingCount)} not in the index, ${String(run.orphanCount)} stored but not selected.`,
    );
    await expect(page.getByTestId("diagnostics-health")).toBeVisible();
    await expect(page.getByTestId("inspector-error")).toHaveCount(0);
    await expect(page.getByTestId("inspector-loading")).toHaveCount(0);

    // Each item is classified once, so categories never exceed the selection.
    expect(run.indexableCount + run.skippedCount).toBe(run.selectedItemCount);
    const counts: Record<Category, number> = {
      skipped: run.skippedCount,
      missing: run.missingCount,
      orphans: run.orphanCount,
    };
    const categories = ["skipped", "missing", "orphans"] as const;
    for (const category of categories) {
      expect(run[category]).toHaveLength(Math.min(counts[category], PAGE_SIZE));
    }
    for (const category of categories.filter((c) => counts[c] === 0)) {
      await expect(
        page.getByTestId(`diagnostics-${category}`).getByRole("button"),
      ).toHaveCount(0);
    }
    for (const category of categories.filter((c) => counts[c] > 0)) {
      const testId = `diagnostics-${category}`;
      const count = counts[category];
      for (const collapsed of await page
        .getByTestId(testId)
        .locator('[role="button"][aria-expanded="false"]')
        .all()) {
        await collapsed.click();
      }
      await expect(range(page, testId)).toHaveText(
        `Showing 1–${String(Math.min(count, PAGE_SIZE))} of ${String(count)} items.`,
      );
      await expect(pager(page, testId)).toHaveCount(Number(count > PAGE_SIZE));
    }
    for (const category of categories.filter((c) => counts[c] > PAGE_SIZE)) {
      const testId = `diagnostics-${category}`;
      const count = counts[category];
      const lastPage = Math.ceil(count / PAGE_SIZE);
      const loaded = page.waitForResponse(
        (response) => commandName(response.request()) === "LoadDiagnosticsPage",
      );
      await showPage(page, testId, String(lastPage));
      const pageResult = (
        (await (await loaded).json()) as {
          result: { total: number; items: DiagnosticItem[] };
        }
      ).result;
      expect(pageResult.total).toBe(count);
      const first = (lastPage - 1) * PAGE_SIZE + 1;
      expect(pageResult.items).toHaveLength(count - first + 1);
      await expect(range(page, testId)).toHaveText(
        `Showing ${String(first)}–${String(count)} of ${String(count)} items.`,
      );
      // Pages do not repeat items from the first page.
      const firstKeys = new Set(
        run[category].map((item) => `${item.itemGuid}|${item.languageName}`),
      );
      expect(
        pageResult.items.filter((item) =>
          firstKeys.has(`${item.itemGuid}|${item.languageName}`),
        ),
      ).toEqual([]);
    }
    await page.setViewportSize({ width: 1280, height: 2400 });
    await page.screenshot({
      path: testInfo.outputPath("status-review.png"),
      fullPage: true,
    });
  });
});
