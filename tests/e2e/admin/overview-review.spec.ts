import {
  expect,
  test,
  type Locator,
  type Page,
  type Request,
} from "@playwright/test";
import { luceneIndexName } from "./luceneIndexesPage";
import { adminBaseUrl } from "../shared/config";

interface Summary {
  indexName: string;
  displayName: string;
  strategyName: string;
  analyzerName: string;
  languages: string[];
  reusableContentTypes: string[];
  channels: {
    channelName: string;
    paths: { aliasPath: string; contentTypes: string[] }[];
  }[];
  contentTypeDisplayNames?: Record<string, string>;
  status: {
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
  };
}

const longTypes = [
  "DancingGoat.ProductAccessory",
  "DancingGoat.ProductBrewer",
  "DancingGoat.ProductCoffee",
  "DancingGoat.ProductGrinder",
  "DancingGoat.ProductTableware",
];

const baseSummary = (): Summary => ({
  indexName: luceneIndexName,
  displayName: "Dancing Goat global search",
  strategyName: "DancingGoatGlobalSearch",
  analyzerName: "Standard",
  languages: ["en", "es"],
  reusableContentTypes: longTypes,
  channels: [
    {
      channelName: "Dancing Goat Pages",
      paths: [
        { aliasPath: "/Articles/%", contentTypes: ["DancingGoat.ArticlePage"] },
        { aliasPath: "/Landing", contentTypes: [] },
      ],
    },
    { channelName: "Empty channel", paths: [] },
  ],
  // DancingGoat.ProductTableware is left out to cover a deleted content type.
  contentTypeDisplayNames: {
    "DancingGoat.ProductAccessory": "Accessory",
    "DancingGoat.ProductBrewer": "Brewer",
    "DancingGoat.ProductCoffee": "Coffee",
    "DancingGoat.ProductGrinder": "Grinder",
    "DancingGoat.ArticlePage": "Article page",
  },
  status: {
    hasPublishedGeneration: true,
    publishedGeneration: 3,
    generations: [{ generation: 3, isPublished: true }],
    documentCount: 1234,
    deletedDocumentCount: 5,
    lastUpdatedUtc: "2026-11-22T12:00:00Z",
    lastRebuildRequest: null,
    readError: null,
  },
});

const displayNameOf = (summary: Summary, codeName: string): string =>
  summary.contentTypeDisplayNames?.[codeName] ?? codeName;

const commandName = (request: Request): string | undefined =>
  /name="command"\r\n\r\n([^\r]+)/.exec(request.postData() ?? "")?.[1];

async function mockSummary(page: Page, result: Summary): Promise<void> {
  await mockCommand(page, "LoadSummary", result);
}

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
    await route.fulfill({ json: { commandName: null, messages: [], result } });
  });
}

async function box(
  locator: Locator,
): Promise<{ x: number; y: number; width: number; height: number }> {
  const value = await locator.boundingBox();
  expect(value).not.toBeNull();
  return value ?? { x: 0, y: 0, width: 0, height: 0 };
}

async function openOverview(page: Page): Promise<void> {
  await page.goto(`${adminBaseUrl}/lucene/indexes`);
  const row = page.getByRole("row").filter({
    has: page.getByRole("cell", { name: luceneIndexName, exact: true }),
  });
  await expect(row).toBeVisible();
  await row.getByRole("link").first().click();
  await page
    .getByTestId("side-navigation-vertical-menu")
    .getByRole("link", { name: "Overview", exact: true })
    .click();
  await expect(page.getByTestId("inspector-overview")).toBeVisible();
}

test.describe("Lucene inspector Overview review", () => {
  test("live overview shows identity, availability and every configured content type without truncation", async ({
    page,
  }, testInfo) => {
    const responsePromise = page.waitForResponse(
      (response) => commandName(response.request()) === "LoadSummary",
    );
    await openOverview(page);
    const envelope = (await (await responsePromise).json()) as {
      result: Summary;
    };
    const summary = envelope.result;
    await expect(page.getByTestId("inspector-loading")).toHaveCount(0);
    await expect(page.getByTestId("inspector-error")).toHaveCount(0);

    await expect(page.getByTestId("overview-display-name")).toHaveText(
      summary.displayName,
    );
    await expect(page.getByTestId("overview-code-name")).toHaveText(
      summary.indexName,
    );
    const main = page;
    await expect(
      main.getByRole("heading", {
        level: 1,
        name: summary.displayName,
        exact: true,
      }),
    ).toHaveCount(1);
    await expect(
      main.getByRole("heading", { name: summary.displayName, exact: true }),
    ).toHaveCount(1);
    for (const section of [
      "Configuration",
      "Index health",
      "Indexing diagnostics",
    ]) {
      await expect(
        main.getByRole("heading", { level: 2, name: section, exact: true }),
      ).toBeVisible();
    }
    for (const subsection of [
      "Reusable content types",
      "Website channel content",
    ]) {
      await expect(
        main.getByRole("heading", { level: 3, name: subsection, exact: true }),
      ).toBeVisible();
    }
    // Value explanations are row tooltips, not a separate section.
    await expect(page.getByText("About these values")).toHaveCount(0);
    await expect(page.getByTestId("status-help")).toHaveCount(
      summary.status.lastRebuildRequest ? 7 : 6,
    );

    // Breadcrumbs and navigation use the saved display name, not the code name.
    const breadcrumbs = page.getByTestId("breadcrumbs");
    await expect(
      breadcrumbs.getByRole("link", { name: summary.displayName, exact: true }),
    ).toBeVisible();
    await expect(
      page.getByTestId("side-navigation-vertical-menu"),
    ).toContainText(summary.displayName);
    expect(summary.displayName).not.toBe(summary.indexName);
    await expect(breadcrumbs).not.toContainText(summary.indexName);

    const identity = page.getByTestId("overview-table");
    await expect(identity).toContainText("Indexing strategy");
    await expect(identity).toContainText(summary.strategyName);
    await expect(identity).toContainText(summary.analyzerName);
    await expect(identity).toContainText(summary.languages.join(", "));

    const health = page.getByTestId("status-health");
    await expect(health).toContainText(
      summary.status.hasPublishedGeneration ? "Searchable" : "Not searchable",
    );
    await expect(page.getByTestId("status-documents")).toHaveText(
      String(summary.status.documentCount),
    );

    const reusable = page.getByTestId("overview-reusable-types");
    await expect(reusable.getByTestId("overview-content-type")).toHaveCount(
      summary.reusableContentTypes.length,
    );
    for (const type of summary.reusableContentTypes) {
      const row = reusable
        .getByTestId("overview-content-type")
        .filter({ hasText: type });
      await expect(row.getByTestId("overview-content-type-code")).toHaveText(
        type,
      );
      await expect(row).toContainText(displayNameOf(summary, type));
    }
    await expect(reusable.locator("[class*='Tag']")).toHaveCount(0);
    await expect(
      page.getByText("Reusable content items of these types are indexed."),
    ).toHaveCount(0);

    const channels = page.getByTestId("overview-channel");
    await expect(channels).toHaveCount(summary.channels.length);
    for (const channel of summary.channels) {
      const region = page.getByRole("region", { name: channel.channelName });
      await expect(
        region.getByRole("heading", { level: 4, name: channel.channelName }),
      ).toBeVisible();
      for (const path of channel.paths) {
        await expect(region).toContainText(path.aliasPath);
        for (const type of path.contentTypes) {
          await expect(region).toContainText(type);
          await expect(region).toContainText(displayNameOf(summary, type));
        }
      }
    }

    // Overview has no disclosure widgets: everything is visible without expanding.
    await expect(
      page.getByTestId("inspector-overview").locator("details, summary"),
    ).toHaveCount(0);

    const screenshotPath = testInfo.outputPath("overview-live.png");
    await page.getByTestId("inspector-overview").screenshot({
      path: screenshotPath,
    });
    await testInfo.attach("overview-live", {
      path: screenshotPath,
      contentType: "image/png",
    });
  });

  test("groups paths per channel and shows empty states", async ({
    page,
  }, testInfo) => {
    await mockSummary(page, baseSummary());
    await openOverview(page);

    await expect(page.getByTestId("overview-display-name")).toHaveText(
      "Dancing Goat global search",
    );
    await expect(page.getByTestId("status-documents")).toHaveText("1234");
    await expect(page.getByTestId("status-deleted")).toHaveText("5");
    await expect(page.getByTestId("status-health")).toContainText(
      "Searchable: generation 3 is published",
    );

    const pages = page.getByRole("region", { name: "Dancing Goat Pages" });
    await expect(pages.getByTestId("overview-path")).toHaveCount(2);
    await expect(pages).toContainText("/Articles/%");
    const articleType = pages.getByTestId("overview-content-type");
    await expect(
      articleType.getByTestId("overview-content-type-display"),
    ).toHaveText("Article page");
    await expect(
      articleType.getByTestId("overview-content-type-code"),
    ).toHaveText("DancingGoat.ArticlePage");
    const reusable = page.getByTestId("overview-reusable-types");
    await expect(reusable.getByTestId("overview-content-type")).toHaveCount(5);
    await expect(
      reusable
        .getByTestId("overview-content-type")
        .filter({ hasText: "DancingGoat.ProductCoffee" }),
    ).toContainText("Coffee");
    // A deleted type has no display name, so only its code name is shown.
    const deleted = reusable
      .getByTestId("overview-content-type")
      .filter({ hasText: "DancingGoat.ProductTableware" });
    await expect(
      deleted.getByTestId("overview-content-type-display"),
    ).toHaveCount(0);
    await expect(deleted.getByTestId("overview-content-type-code")).toHaveText(
      "DancingGoat.ProductTableware",
    );
    await expect(reusable.locator("[class*='Tag']")).toHaveCount(0);

    // The admin shell's inherited text color is red; plain text must use default text tokens.
    const color = (locator: Locator): Promise<string> =>
      locator.evaluate((element) => getComputedStyle(element).color);
    const nativeText = await color(
      page.getByTestId("overview-table").getByText("Standard", { exact: true }),
    );
    expect(nativeText).not.toBe("rgb(255, 0, 0)");
    for (const plain of [
      articleType.getByTestId("overview-content-type-display"),
      deleted.getByTestId("overview-content-type-code"),
      pages.getByRole("heading", { level: 4 }),
      page.getByTestId("overview-display-name"),
    ]) {
      expect(await color(plain)).toBe(nativeText);
    }
    const muted = await color(
      articleType.getByTestId("overview-content-type-code"),
    );
    expect(muted).not.toBe(nativeText);
    expect(await color(page.getByTestId("overview-code-name"))).toBe(muted);

    // Code names are smaller secondary text next to their display names.
    const fontSize = async (locator: Locator): Promise<number> =>
      parseFloat(
        await locator.evaluate((element) => getComputedStyle(element).fontSize),
      );
    expect(
      await fontSize(articleType.getByTestId("overview-content-type-code")),
    ).toBe(12);
    expect(await fontSize(page.getByTestId("overview-code-name"))).toBe(12);
    expect(
      await fontSize(articleType.getByTestId("overview-content-type-display")),
    ).toBeGreaterThan(12);
    // A code-only fallback is the primary text, so it keeps the normal size.
    expect(
      await fontSize(deleted.getByTestId("overview-content-type-code")),
    ).toBeGreaterThan(12);
    expect(
      await color(pages.getByText("Content types", { exact: true }).first()),
    ).toBe(muted);

    // Equivalent subsections use the same heading-to-content spacing.
    const reusableHeading = await box(
      page.getByRole("heading", { level: 3, name: "Reusable content types" }),
    );
    const reusableList = await box(reusable);
    const channelHeading = await box(
      page.getByRole("heading", { level: 3, name: "Website channel content" }),
    );
    const channelCount = await box(
      page.getByText("2 channels, 2 included paths"),
    );
    const reusableGap =
      reusableList.y - (reusableHeading.y + reusableHeading.height);
    const channelGap =
      channelCount.y - (channelHeading.y + channelHeading.height);
    expect(reusableGap).toBeGreaterThan(4);
    expect(Math.abs(reusableGap - channelGap)).toBeLessThanOrEqual(1);

    // Type lists show a label and rows, without TextWithLabel's "-" placeholder.
    await expect(pages.getByTestId("overview-path-types").first()).toHaveText(
      "Content typesArticle pageDancingGoat.ArticlePage",
    );
    await expect(
      pages
        .getByTestId("overview-path")
        .nth(1)
        .getByTestId("overview-path-types"),
    ).toContainText("No content types selected.");
    await expect(
      page.getByRole("region", { name: "Empty channel" }),
    ).toContainText("No included paths.");
    await expect(page.getByTestId("overview-channels")).toContainText(
      "Dancing Goat Pages",
    );

    const screenshotPath = testInfo.outputPath("overview-mocked.png");
    await page.getByTestId("overview-channels").screenshot({
      path: screenshotPath,
    });
    await testInfo.attach("overview-mocked", {
      path: screenshotPath,
      contentType: "image/png",
    });
  });

  test("shows explicit empty configuration messages", async ({ page }) => {
    await mockSummary(page, {
      ...baseSummary(),
      languages: [],
      reusableContentTypes: [],
      channels: [],
      status: {
        ...baseSummary().status,
        hasPublishedGeneration: false,
        publishedGeneration: null,
        generations: [],
        documentCount: 0,
        deletedDocumentCount: 0,
        lastUpdatedUtc: null,
      },
    });
    await openOverview(page);

    await expect(page.getByTestId("overview-table")).toContainText(
      "No languages selected",
    );
    await expect(page.getByTestId("overview-reusable-types")).toHaveText(
      "No reusable content types are indexed.",
    );
    await expect(page.getByTestId("overview-channels")).toHaveText(
      "No website channel content is indexed.",
    );
    await expect(page.getByTestId("status-health")).toContainText(
      "Not searchable",
    );
    await expect(page.getByTestId("status-published")).toHaveText("None");
    await expect(page.getByTestId("status-updated")).toHaveText("Unavailable");
  });

  test("keeps the configuration visible when the index files cannot be read", async ({
    page,
  }) => {
    await mockSummary(page, {
      ...baseSummary(),
      status: {
        ...baseSummary().status,
        readError: "Overview regression read error",
      },
    });
    await openOverview(page);

    await expect(page.getByRole("alert")).toContainText(
      "Overview regression read error",
    );
    await expect(page.getByTestId("overview-code-name")).toHaveText(
      luceneIndexName,
    );
    await expect(page.getByTestId("status-health")).toContainText(
      "is published but cannot be read",
    );
    await expect(page.getByTestId("status-documents")).toHaveText(
      "Unavailable",
    );
    await expect(page.getByTestId("overview-channels")).toContainText(
      "/Articles/%",
    );
  });

  test("puts configuration and index health side by side on wide screens and stacks them on narrow screens", async ({
    page,
  }, testInfo) => {
    await mockSummary(page, baseSummary());
    await page.setViewportSize({ width: 1600, height: 1400 });
    await openOverview(page);
    const configuration = page.getByTestId("overview-configuration");
    const health = page.getByTestId("overview-health");
    await expect(
      configuration.getByRole("region", { name: "Configuration" }),
    ).toBeVisible();
    await expect(
      health.getByRole("region", { name: "Index health" }),
    ).toBeVisible();
    await expect(health.getByTestId("status-table")).toBeVisible();

    const wideConfig = await box(configuration);
    const wideHealth = await box(health);
    expect(Math.abs(wideConfig.y - wideHealth.y)).toBeLessThan(2);
    expect(wideHealth.x).toBeGreaterThanOrEqual(
      wideConfig.x + wideConfig.width - 1,
    );
    expect(wideHealth.width).toBeGreaterThan(300);
    const runButton = await page
      .getByRole("button", { name: "Run diagnostics", exact: true })
      .boundingBox();
    expect(runButton?.y ?? 0).toBeGreaterThan(
      Math.max(
        wideConfig.y + wideConfig.height,
        wideHealth.y + wideHealth.height,
      ) - 1,
    );
    await page.screenshot({
      path: testInfo.outputPath("overview-wide.png"),
      fullPage: true,
    });

    await page.setViewportSize({ width: 1100, height: 1400 });
    await expect
      .poll(async () => {
        const config = await configuration.boundingBox();
        const status = await health.boundingBox();
        return config && status
          ? status.y >= config.y + config.height - 1
          : false;
      })
      .toBe(true);
    const narrowConfig = await configuration.boundingBox();
    const narrowHealth = await health.boundingBox();
    expect(
      Math.abs((narrowConfig?.x ?? 0) - (narrowHealth?.x ?? 1)),
    ).toBeLessThan(2);
    await page.screenshot({
      path: testInfo.outputPath("overview-narrow.png"),
      fullPage: true,
    });
  });

  test("Rebuild is right-aligned in Index health, cancel sends nothing and failures are explicit", async ({
    page,
  }) => {
    await mockSummary(page, baseSummary());
    await mockCommand(page, "LoadRebuildAccess", { canRebuild: true });
    await mockCommand(page, "RebuildIndex", {
      requested: false,
      error: "Overview regression rebuild failure",
    });
    const rebuildRequests: Request[] = [];
    page.on("request", (request) => {
      if (commandName(request) === "RebuildIndex") {
        rebuildRequests.push(request);
      }
    });
    await page.setViewportSize({ width: 1600, height: 1400 });
    await openOverview(page);

    const health = page.getByTestId("overview-health");
    const rebuild = health.getByRole("button", { name: "Rebuild" });
    await expect(rebuild).toBeEnabled();
    const button = await box(rebuild);
    const heading = await box(
      health.getByRole("heading", { level: 2, name: "Index health" }),
    );
    const table = await box(health.getByTestId("status-table"));
    expect(
      Math.abs(button.x + button.width - (table.x + table.width)),
    ).toBeLessThanOrEqual(2);
    expect(button.x).toBeGreaterThan(heading.x + heading.width);
    expect(button.y).toBeLessThan(heading.y + heading.height);
    expect(button.y + button.height).toBeGreaterThan(heading.y);

    await rebuild.click();
    const dialog = page.getByRole("dialog");
    await expect(dialog).toContainText("Rebuild index?");
    await expect(dialog).toContainText(
      "searches can return incomplete or no results",
    );
    await expect(dialog).toContainText(
      "Progress and completion are not tracked",
    );
    await dialog.getByRole("button", { name: /cancel/i }).click();
    await expect(dialog).toHaveCount(0);
    await expect(page.getByTestId("overview-rebuild-message")).toHaveCount(0);
    expect(rebuildRequests).toHaveLength(0);

    await rebuild.click();
    await page
      .getByRole("dialog")
      .getByRole("button", { name: /^rebuild$/i })
      .click();
    const message = page.getByTestId("overview-rebuild-message");
    await expect(message).toHaveText("Overview regression rebuild failure");
    await expect(message).not.toContainText("Rebuild requested");
    expect(rebuildRequests).toHaveLength(1);
    await expect(rebuild).toBeEnabled();
  });

  test("Rebuild is disabled without the rebuild permission", async ({
    page,
  }) => {
    await mockSummary(page, baseSummary());
    await mockCommand(page, "LoadRebuildAccess", { canRebuild: false });
    await openOverview(page);
    await expect(
      page
        .getByTestId("overview-health")
        .getByRole("button", { name: "Rebuild" }),
    ).toBeDisabled();
    await expect(page.getByTestId("overview-rebuild-message")).toHaveText(
      "You don't have permission to rebuild this index.",
    );
  });

  test("live Rebuild reports only the request and refreshes index health", async ({
    page,
  }) => {
    await openOverview(page);
    const health = page.getByTestId("overview-health");
    const rebuild = health.getByRole("button", { name: "Rebuild" });
    await expect(rebuild).toBeEnabled();
    await rebuild.click();

    const response = page.waitForResponse(
      (candidate) => commandName(candidate.request()) === "RebuildIndex",
    );
    const refreshed = page.waitForRequest(
      (request) => commandName(request) === "LoadSummary",
    );
    await page
      .getByRole("dialog")
      .getByRole("button", { name: /^rebuild$/i })
      .click();
    const { result } = (await (await response).json()) as {
      result: { requested: boolean; error: string | null };
    };
    expect(result).toEqual({ requested: true, error: null });
    await refreshed;

    const message = page.getByTestId("overview-rebuild-message");
    await expect(message).toContainText("Rebuild requested");
    await expect(message).toContainText("Completion is not tracked");
    await expect(message).not.toContainText(/rebuilt|completed/i);
    await expect(page.getByRole("dialog")).toHaveCount(0);
    await expect(page.getByTestId("status-rebuild")).not.toHaveText(
      "None since this server started",
    );
    await expect(page.getByTestId("status-rebuild-queued")).toHaveText(
      /^\d[\d,]*$/,
    );

    // Leave the shared sample searchable for the next suites.
    await expect
      .poll(
        async () => {
          await page
            .getByRole("button", { name: "Refresh status", exact: true })
            .click();
          await expect(page.getByTestId("inspector-loading")).toHaveCount(0);
          return (await page.getByTestId("status-health").innerText()).trim();
        },
        { timeout: 60_000, intervals: [2_000] },
      )
      .toMatch(/^Searchable/);
    await expect(page.getByTestId("status-documents")).not.toHaveText("0");
  });
});
