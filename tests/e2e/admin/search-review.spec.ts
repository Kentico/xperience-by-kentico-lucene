import {
  expect,
  test,
  type Locator,
  type Page,
  type Request,
} from "@playwright/test";
import { luceneIndexName } from "./luceneIndexesPage";
import { adminBaseUrl } from "../shared/config";

const pageSize = 20;
const readOnlyCommands = [
  "LoadSummary",
  "LoadSearchFields",
  "LoadDocuments",
  "RunQuery",
  "RunFilterQuery",
];

interface StoredField {
  name: string;
  value: string;
}

interface QueryResult {
  parsedQuery: string | null;
  totalHits: number;
  hits: { document: { fields: StoredField[] } }[];
  error: string | null;
}

const commandName = (request: Request): string | undefined =>
  /name="command"\r\n\r\n([^\r]+)/.exec(request.postData() ?? "")?.[1];

const commandData = (request: Request): Record<string, unknown> =>
  JSON.parse(
    /name="data"\r\n\r\n([^\r]+)/.exec(request.postData() ?? "")?.[1] ?? "{}",
  ) as Record<string, unknown>;

const isCommand = (request: Request, name: string): boolean =>
  request.url().endsWith("/api/cmd") && commandName(request) === name;

const storedDocument = (id: number) => ({
  documentId: id,
  fields: [
    {
      name: "ItemGuid",
      value: `00000000-0000-0000-0000-${String(id).padStart(12, "0")}`,
    },
    { name: "LanguageName", value: "en" },
    { name: "ContentTypeName", value: "DancingGoat.ArticlePage" },
    { name: "Title", value: `Mocked document ${String(id)}` },
    { name: "Url", value: `~/articles/mocked-${String(id)}` },
  ],
});

const storedValue = (hit: QueryResult["hits"][number], name: string): string =>
  hit.document.fields.find((f) => f.name === name)?.value ?? "";

const storedValues = (result: QueryResult, name: string): string[] =>
  result.hits.map((hit) => storedValue(hit, name));

function recordCommands(page: Page): string[] {
  const commands: string[] = [];
  page.on("request", (request) => {
    const name = request.url().endsWith("/api/cmd")
      ? commandName(request)
      : undefined;
    if (name) {
      commands.push(name);
    }
  });
  return commands;
}

/** Opens the Search tab and returns the stored document total of the first page. */
async function openSearch(page: Page): Promise<number> {
  const documents = page.waitForResponse((r) =>
    isCommand(r.request(), "LoadDocuments"),
  );
  await page.goto(`${adminBaseUrl}/lucene/indexes`);
  const row = page.getByRole("row").filter({
    has: page.getByRole("cell", { name: luceneIndexName, exact: true }),
  });
  await expect(row).toBeVisible();
  await row.getByRole("link").first().click();
  const menu = page.getByTestId("side-navigation-vertical-menu");
  await expect(menu.getByRole("link", { name: "Documents" })).toHaveCount(0);
  await expect(menu.getByRole("link", { name: "Test search" })).toHaveCount(0);
  await menu.getByRole("link", { name: "Search", exact: true }).click();
  await expect(page.getByTestId("inspector-search")).toBeVisible();
  const { result } = (await (await documents).json()) as {
    result: { total: number };
  };
  return result.total;
}

const advancedMode = (page: Page): Locator =>
  page.getByRole("radio", { name: "Lucene query (advanced)", exact: true });

// The native radio input sits under its decorative circle, so select it through its visible label.
async function chooseAdvancedMode(page: Page): Promise<void> {
  await page.getByText("Lucene query (advanced)", { exact: true }).click();
  await expect(advancedMode(page)).toBeChecked();
}

async function search(page: Page, query: string, field = ""): Promise<void> {
  if (!(await advancedMode(page).isChecked())) {
    await chooseAdvancedMode(page);
  }
  await page.getByLabel("Query", { exact: true }).fill(query);
  await page.getByLabel("Default field", { exact: true }).fill(field);
  await page.getByRole("button", { name: "Search", exact: true }).click();
  await expect(page.getByTestId("inspector-loading")).toHaveCount(0);
}

const filterRows = (page: Page): Locator => page.getByTestId("search-filter");

const filterValue = (row: Locator): Locator => row.locator("input").nth(1);

async function chooseMenuItem(page: Page, label: string): Promise<void> {
  await page
    .getByTestId("menu-item")
    .filter({ has: page.getByText(label, { exact: true }) })
    .click();
  await expect(page.getByTestId("menu-item")).toHaveCount(0);
}

/** Adds a filter row for the field display name and optionally types its value. */
async function addFilter(
  page: Page,
  field: string,
  value?: string,
): Promise<Locator> {
  await page.getByRole("button", { name: "Add filter", exact: true }).click();
  const row = filterRows(page).last();
  await row.locator("input").first().click();
  await chooseMenuItem(page, field);
  await expect(row).toHaveAttribute(
    "aria-label",
    `Filter ${String(await filterRows(page).count())}: ${field}`,
  );
  if (value !== undefined) {
    await filterValue(row).fill(value);
  }
  return row;
}

async function runFilters(page: Page): Promise<QueryResult> {
  const response = page.waitForResponse((r) =>
    isCommand(r.request(), "RunFilterQuery"),
  );
  await page.getByRole("button", { name: "Search", exact: true }).click();
  const { result } = (await (await response).json()) as {
    result: QueryResult;
  };
  await expect(page.getByTestId("inspector-loading")).toHaveCount(0);
  expect(result.error).toBeNull();
  await expect(page.getByTestId("search-total")).toHaveText(
    result.totalHits === 0
      ? "0 matching documents."
      : `Showing 1–${String(Math.min(pageSize, result.totalHits))} of ${String(result.totalHits)} matching document${result.totalHits === 1 ? "" : "s"}, highest score first.`,
  );
  return result;
}

const resultRows = (page: Page): Locator =>
  page.getByTestId("search-results").getByTestId("table-row");

const paginationButton = (page: Page, number: number): Locator =>
  page
    .getByTestId("search-pages")
    .getByRole("button", { name: String(number), exact: true });

async function bounds(
  locator: Locator,
): Promise<{ x: number; y: number; width: number; height: number }> {
  const box = await locator.boundingBox();
  if (!box) {
    throw new Error("The element must have visible bounds.");
  }
  return box;
}

test.describe("Lucene inspector search", () => {
  test("content type names stay compact, left-aligned and inside result rows", async ({
    page,
  }) => {
    await openSearch(page);

    const checkContentTypes = async (): Promise<void> => {
      const cells = resultRows(page)
        .getByTestId("table-cell-type")
        .filter({ has: page.getByTestId("code-name") });
      await expect(cells.first()).toBeVisible();
      for (const cell of await cells.all()) {
        const codeName = cell.getByTestId("code-name");
        const primary = cell.locator("span").first().locator("span").first();
        const primaryBox = await bounds(primary);
        const codeBox = await bounds(codeName);
        const cellBox = await bounds(cell);
        expect(
          await primary.evaluate(
            (element) => getComputedStyle(element).textAlign,
          ),
        ).toBe("left");
        expect(primaryBox.height).toBeLessThanOrEqual(20);
        expect(Math.abs(primaryBox.x - codeBox.x)).toBeLessThanOrEqual(1);
        expect(
          codeBox.y - (primaryBox.y + primaryBox.height),
        ).toBeGreaterThanOrEqual(0);
        expect(
          codeBox.y - (primaryBox.y + primaryBox.height),
        ).toBeLessThanOrEqual(4);
        expect(primaryBox.y).toBeGreaterThanOrEqual(cellBox.y);
        expect(codeBox.y + codeBox.height).toBeLessThanOrEqual(
          cellBox.y + cellBox.height,
        );
      }
      await expect(cells.getByTestId("code-name").first()).toBeVisible();
    };

    await checkContentTypes();
    await addFilter(page, "Content", "brew*");
    const result = await runFilters(page);
    expect(result.totalHits).toBeGreaterThan(0);
    await checkContentTypes();
  });

  test("blank query lists every stored record in a paged compact table", async ({
    page,
  }, testInfo) => {
    const first = page.waitForRequest((r) => isCommand(r, "LoadDocuments"));
    const total = await openSearch(page);
    expect(commandData(await first)).toMatchObject({
      skip: 0,
      take: pageSize,
    });
    expect(total).toBeGreaterThan(pageSize);
    const pages = Math.ceil(total / pageSize);

    await expect(
      page.getByRole("radio", { name: "Filters", exact: true }),
    ).toBeChecked();
    await expect(page.getByLabel("Query", { exact: true })).toHaveCount(0);
    await expect(page.getByTestId("search-total")).toHaveText(
      `Showing 1–${String(pageSize)} of ${String(total)} stored documents.`,
    );
    await expect(resultRows(page)).toHaveCount(pageSize);
    await expect(page.getByText(/Select a row/)).toHaveCount(0);
    const pageTitle = page.getByRole("heading", { level: 1, name: "Search" });
    const resultsHeading = page.getByRole("heading", {
      level: 2,
      name: "Stored documents",
    });
    await expect(pageTitle).toBeVisible();
    await expect(resultsHeading).toBeVisible();
    const fontSize = (locator: Locator): Promise<number> =>
      locator.evaluate((e) => {
        let el: Element = e;
        while (el.firstElementChild) {
          el = el.firstElementChild;
        }
        return parseFloat(getComputedStyle(el).fontSize);
      });
    expect(await fontSize(resultsHeading)).toBeLessThan(
      await fontSize(pageTitle),
    );
    await expect(
      page.getByTestId("search-results").getByRole("heading"),
    ).toHaveCount(0);
    const resultsBox = await bounds(page.getByTestId("search-results"));
    const totalBox = await bounds(page.getByTestId("search-total"));
    const pagesBox = await bounds(page.getByTestId("search-pages"));
    expect(totalBox.y).toBeGreaterThan(resultsBox.y + resultsBox.height);
    expect(totalBox.y).toBeGreaterThanOrEqual(pagesBox.y);
    const header = page.getByTestId("search-results").getByRole("columnheader");
    await expect(header).toHaveText([
      "#",
      "Title",
      "Content type",
      "Language",
      "URL",
      "Actions",
    ]);
    await expect(page.locator("details, summary")).toHaveCount(0);
    await expect(
      resultRows(page).first().getByTestId("table-cell-language"),
    ).not.toBeEmpty();
    await page.getByTestId("search-results").screenshot({
      path: testInfo.outputPath("search-browse.png"),
    });

    const firstTitle = await resultRows(page)
      .first()
      .getByTestId("table-cell-title")
      .innerText();
    const second = page.waitForRequest((r) => isCommand(r, "LoadDocuments"));
    await paginationButton(page, 2).click();
    expect(commandData(await second)).toMatchObject({
      skip: pageSize,
      take: pageSize,
    });
    await expect(page.getByTestId("search-total")).toContainText(
      `Showing ${String(pageSize + 1)}–`,
    );
    await expect(
      resultRows(page).first().getByTestId("table-cell-position"),
    ).toHaveText(String(pageSize + 1));
    await expect(
      resultRows(page).first().getByTestId("table-cell-title"),
    ).not.toHaveText(firstTitle);

    await paginationButton(page, pages).click();
    const remainder = total - (pages - 1) * pageSize;
    await expect(page.getByTestId("search-total")).toHaveText(
      `Showing ${String((pages - 1) * pageSize + 1)}–${String(total)} of ${String(total)} stored documents.`,
    );
    await expect(resultRows(page)).toHaveCount(remainder);
  });

  test("selecting a row opens its stored record in a side panel", async ({
    page,
  }, testInfo) => {
    await openSearch(page);
    const row = resultRows(page).first();
    const title = await row.getByTestId("table-cell-title").innerText();
    const viewButton = row.getByRole("button", {
      name: `View stored record of ${title}`,
      exact: true,
    });
    await expect(viewButton).toBeVisible();
    await row.getByTestId("table-cell-title").click();

    const panel = page.getByTestId("search-panel");
    await expect(panel).toBeVisible();
    await expect(page.getByTestId("search-panel-title")).toHaveText(title);
    await expect(
      page.getByRole("heading", { level: 2, name: title, exact: true }),
    ).toBeVisible();
    await expect(panel.getByRole("heading", { level: 3 })).toHaveText([
      "Metadata",
      /^Stored fields \(\d+\)$/,
    ]);
    const innermostFontSize = (locator: Locator): Promise<number> =>
      locator.evaluate((e) => {
        let el: Element = e;
        while (el.firstElementChild) {
          el = el.firstElementChild;
        }
        return parseFloat(getComputedStyle(el).fontSize);
      });
    expect(
      await innermostFontSize(page.getByTestId("search-panel-title")),
    ).toBeGreaterThan(
      await innermostFontSize(panel.getByRole("heading", { level: 3 }).first()),
    );
    const textColor = (locator: Locator): Promise<string> =>
      locator.evaluate((e) => getComputedStyle(e).color);
    const defaultColor = await textColor(
      page.getByRole("heading", { level: 1, name: "Search" }),
    );
    for (const text of [
      page.getByTestId("search-panel-title").getByText(title, { exact: true }),
      page
        .getByTestId("search-panel-fields")
        .getByText(title, { exact: true })
        .first(),
      panel,
    ]) {
      expect(await textColor(text)).toBe(defaultColor);
    }
    const metadata = page.getByTestId("search-panel-metadata");
    for (const label of [
      "Position",
      "Language",
      "Content type",
      "URL",
      "Item GUID",
      "Lucene document",
    ]) {
      await expect(metadata.getByText(label, { exact: true })).toBeVisible();
    }
    await expect(metadata.getByText("Score", { exact: true })).toHaveCount(0);
    const fields = page.getByTestId("search-panel-fields");
    await expect(fields).toContainText(/Stored fields \(\d+\)/);
    await expect(fields.getByText("ItemGuid", { exact: true })).toBeVisible();
    await expect(fields.getByRole("heading", { name: "ItemGuid" })).toHaveCount(
      0,
    );
    await expect(page.getByTestId("search-panel-explanation")).toHaveCount(0);
    await page.getByTestId("side-panel").screenshot({
      path: testInfo.outputPath("search-panel-browse.png"),
      animations: "disabled",
    });

    await page
      .getByTestId("side-panel-footer")
      .getByRole("button", { name: "Close", exact: true })
      .click();
    await expect(panel).toHaveCount(0);
    await expect(viewButton).toBeFocused();

    await page.keyboard.press("Enter");
    await expect(panel).toBeVisible();
    await page.keyboard.press("Escape");
    await expect(panel).toHaveCount(0);

    await resultRows(page)
      .nth(2)
      .getByRole("button", { name: /^View stored record of / })
      .click();
    await expect(panel).toBeVisible();
    await page.getByTestId("side-panel-close").click();
    await expect(panel).toHaveCount(0);
  });

  test("an advanced query filters matches, explains scores and clears back to all records", async ({
    page,
  }, testInfo) => {
    await openSearch(page);
    const commands = recordCommands(page);
    await expect(page.getByTestId("search-context")).toContainText("analyzer");
    await chooseAdvancedMode(page);
    await expect(page.getByTestId("search-filters")).toHaveCount(0);
    await expect(page.getByTestId("search-fields")).toContainText("Title");

    await search(page, "coffee");
    await expect(page.getByTestId("search-total")).toHaveText(
      "0 matching documents.",
    );
    await expect(page.getByTestId("search-parsed")).toHaveText("Name:coffee");
    await expect(page.getByTestId("search-empty")).toContainText(
      "Title:coffee",
    );

    await search(page, "COFFEE", "Title");
    await expect(page.getByTestId("search-parsed")).toHaveText("Title:coffee");
    await search(page, "Title:coffee");
    await expect(page.getByTestId("search-parsed")).toHaveText("Title:coffee");
    await expect(page.getByTestId("search-total")).toHaveText(
      /^Showing 1–\d+ of \d+ matching documents?, highest score first\.$/,
    );
    await expect(
      page.getByTestId("search-results").getByRole("columnheader"),
    ).toHaveText([
      "#",
      "Title",
      "Content type",
      "Language",
      "Score",
      "URL",
      "Actions",
    ]);
    await expect(
      page.getByRole("heading", { level: 2, name: "Matching documents" }),
    ).toBeVisible();
    const top = resultRows(page).first();
    const queryResultsBox = await bounds(page.getByTestId("search-results"));
    const queryTotalBox = await bounds(page.getByTestId("search-total"));
    expect(queryTotalBox.y).toBeGreaterThan(
      queryResultsBox.y + queryResultsBox.height,
    );
    await expect(top.getByTestId("table-cell-title")).toContainText(/coffee/i);
    await expect(top.getByTestId("table-cell-score")).toHaveText(
      /^\d+\.\d{4}$/,
    );
    const score = await top.getByTestId("table-cell-score").innerText();
    await page.getByTestId("search-results").screenshot({
      path: testInfo.outputPath("search-query.png"),
    });

    await top.getByTestId("table-cell-title").click();
    await expect(
      page.getByTestId("search-panel-metadata").getByText(score, {
        exact: true,
      }),
    ).toBeVisible();
    await expect(page.getByTestId("search-explain")).toContainText("weight(");
    await expect(
      page.getByTestId("search-panel").getByRole("heading", { level: 3 }),
    ).toHaveText(["Metadata", /^Stored fields \(\d+\)$/, "Score explanation"]);
    await page.getByTestId("side-panel").screenshot({
      path: testInfo.outputPath("search-panel-query.png"),
      animations: "disabled",
    });
    await page.getByTestId("side-panel-close").click();

    await page.getByRole("button", { name: "Clear", exact: true }).click();
    await expect(page.getByLabel("Query", { exact: true })).toHaveValue("");
    await expect(page.getByTestId("search-total")).toContainText(
      "stored documents.",
    );
    await expect(page.getByTestId("search-parsed")).toHaveCount(0);
    expect(commands.length).toBeGreaterThan(0);
    expect(commands.every((c) => readOnlyCommands.includes(c))).toBe(true);
  });

  test("pages live query matches beyond the first page", async ({ page }) => {
    await openSearch(page);
    const response = page.waitForResponse((r) =>
      isCommand(r.request(), "RunQuery"),
    );
    await search(page, "*:*");
    const { result } = (await (await response).json()) as {
      result: { totalHits: number };
    };
    expect(result.totalHits).toBeGreaterThan(pageSize);
    const firstTitle = await resultRows(page)
      .first()
      .getByTestId("table-cell-title")
      .innerText();

    const next = page.waitForRequest((r) => isCommand(r, "RunQuery"));
    await paginationButton(page, 2).click();
    expect(commandData(await next)).toMatchObject({
      query: "*:*",
      skip: pageSize,
      take: pageSize,
    });
    await expect(page.getByTestId("search-total")).toContainText(
      `Showing ${String(pageSize + 1)}–`,
    );
    await expect(
      resultRows(page).first().getByTestId("table-cell-position"),
    ).toHaveText(String(pageSize + 1));
    await expect(
      resultRows(page).first().getByTestId("table-cell-title"),
    ).not.toHaveText(firstTitle);
  });

  test("pages results beyond the 50 document request limit", async ({
    page,
  }) => {
    const total = 125;
    await page.route("**/api/cmd", async (route) => {
      const name = commandName(route.request());
      if (name === "LoadSummary") {
        const response = await route.fetch();
        const body = (await response.json()) as {
          result: { contentTypeDisplayNames?: Record<string, string> };
        };
        body.result.contentTypeDisplayNames = {
          "dancinggoat.articlepage": "Article page",
        };
        await route.fulfill({ response, json: body });
        return;
      }
      if (name !== "LoadDocuments" && name !== "RunQuery") {
        await route.fallback();
        return;
      }
      const { skip } = commandData(route.request()) as { skip: number };
      const documents = Array.from(
        { length: Math.min(pageSize, total - skip) },
        (_, i) => {
          const document = storedDocument(skip + i + 1);
          if (i === 1) {
            document.fields[2].value = "DancingGoat.RemovedType";
          }
          return document;
        },
      );
      await route.fulfill({
        json: {
          commandName: null,
          messages: [],
          result:
            name === "LoadDocuments"
              ? { total, documents, readError: null }
              : {
                  parsedQuery: "Title:mocked",
                  totalHits: total,
                  hits: documents.map((document) => ({
                    document,
                    score: 1,
                    explanation: "1.0 = mocked",
                  })),
                  error: null,
                },
        },
      });
    });
    await openSearch(page);
    await expect(page.getByTestId("search-total")).toHaveText(
      "Showing 1–20 of 125 stored documents.",
    );
    // A known type shows its display name with the code name as secondary text; an unknown type falls back to its code name.
    const knownType = resultRows(page).first().getByTestId("table-cell-type");
    await expect(
      knownType.getByText("Article page", { exact: true }),
    ).toBeVisible();
    await expect(knownType.getByTestId("code-name")).toHaveText(
      "DancingGoat.ArticlePage",
    );
    const removedType = resultRows(page).nth(1).getByTestId("table-cell-type");
    await expect(removedType).toHaveText("DancingGoat.RemovedType");
    await expect(removedType.getByTestId("code-name")).toHaveCount(0);
    await resultRows(page).first().getByTestId("button-view").click();
    const metadata = page.getByTestId("search-panel-metadata");
    await expect(
      metadata.getByText("Article page", { exact: true }),
    ).toBeVisible();
    await expect(metadata.getByTestId("code-name")).toHaveText(
      "DancingGoat.ArticlePage",
    );
    // Stored fields keep the raw stored value.
    await expect(
      page
        .getByTestId("search-panel-fields")
        .getByText("DancingGoat.ArticlePage", { exact: true }),
    ).toBeVisible();
    await expect(
      page.getByTestId("search-panel-fields").getByTestId("code-name"),
    ).toHaveCount(0);
    await page.getByTestId("side-panel-close").click();
    await expect(page.getByTestId("search-panel")).toHaveCount(0);
    const last = page.waitForRequest((r) => isCommand(r, "LoadDocuments"));
    await paginationButton(page, 7).click();
    expect(commandData(await last)).toMatchObject({ skip: 120, take: 20 });
    await expect(page.getByTestId("search-total")).toHaveText(
      "Showing 121–125 of 125 stored documents.",
    );
    await expect(resultRows(page)).toHaveCount(5);

    await search(page, "Title:mocked");
    await expect(page.getByTestId("search-total")).toHaveText(
      "Showing 1–20 of 125 matching documents, highest score first.",
    );
    const queryPage = page.waitForRequest((r) => isCommand(r, "RunQuery"));
    await paginationButton(page, 4).click();
    expect(commandData(await queryPage)).toMatchObject({
      query: "Title:mocked",
      skip: 60,
      take: 20,
    });
    await expect(
      resultRows(page).first().getByTestId("table-cell-title"),
    ).toHaveText("Mocked document 61");
  });

  test("reports invalid queries and network failures with retry", async ({
    page,
  }) => {
    await openSearch(page);
    await search(page, "Title:(");
    await expect(page.getByTestId("inspector-error")).toContainText(
      "Invalid query",
    );
    await expect(page.getByTestId("search-results")).toHaveCount(0);
    await expect(
      page.getByRole("button", { name: "Search", exact: true }),
    ).toBeEnabled();
    await search(page, "Title:coffee");
    await expect(page.getByTestId("inspector-error")).toHaveCount(0);
    await expect(resultRows(page).first()).toBeVisible();

    await page.route("**/api/cmd", async (route) => {
      if (commandName(route.request()) === "RunQuery") {
        await route.abort("failed");
      } else {
        await route.fallback();
      }
    });
    await search(page, "Title:coffee");
    await expect(page.getByTestId("inspector-error")).toBeVisible();
    await page.unroute("**/api/cmd");
    await page.getByRole("button", { name: "Retry", exact: true }).click();
    await expect(page.getByTestId("inspector-error")).toHaveCount(0);
    await expect(resultRows(page).first()).toBeVisible();
  });

  test("warns when the index has no published generation", async ({ page }) => {
    await page.route("**/api/cmd", async (route) => {
      if (commandName(route.request()) === "LoadSummary") {
        const response = await route.fetch();
        const body = (await response.json()) as {
          result: { status: Record<string, unknown> };
        };
        body.result.status.hasPublishedGeneration = false;
        body.result.status.publishedGeneration = null;
        await route.fulfill({ response, json: body });
      } else if (commandName(route.request()) === "LoadDocuments") {
        await route.fulfill({
          json: {
            commandName: null,
            messages: [],
            result: { total: 0, documents: [], readError: null },
          },
        });
      } else {
        await route.fallback();
      }
    });
    await openSearch(page);
    await expect(page.getByTestId("search-unpublished")).toContainText(
      "no published generation",
    );
    await expect(page.getByTestId("search-total")).toHaveText(
      "0 stored documents.",
    );
    await expect(page.getByTestId("search-empty")).toContainText(
      "no stored documents",
    );
  });

  test("typed filters must all match and keyword wildcards keep the indexed case", async ({
    page,
  }) => {
    const total = await openSearch(page);
    const commands = recordCommands(page);
    await expect(filterRows(page)).toHaveCount(0);

    await addFilter(page, "Language", "en");
    const english = await runFilters(page);
    expect(english.totalHits).toBeGreaterThan(0);
    expect(english.totalHits).toBeLessThanOrEqual(total);
    expect(new Set(storedValues(english, "LanguageName"))).toEqual(
      new Set(["en"]),
    );

    await addFilter(page, "URL", "~/articles/*");
    const englishArticles = await runFilters(page);
    await expect(page.getByTestId("search-parsed")).toHaveText(
      "+LanguageName:en +Url:~/articles/*",
    );
    expect(englishArticles.totalHits).toBeGreaterThan(0);
    for (const hit of englishArticles.hits) {
      expect(storedValue(hit, "LanguageName")).toBe("en");
      expect(storedValue(hit, "Url")).toMatch(/^~\/articles\//);
    }

    await page
      .getByRole("button", { name: "Remove filter 1: Language", exact: true })
      .click();
    const url = page.getByRole("group", { name: "Filter 1: URL", exact: true });
    await expect(url).toBeVisible();
    const articles = await runFilters(page);
    // Both filters must match, so adding the language filter can only narrow the URL matches.
    expect(englishArticles.totalHits).toBeLessThanOrEqual(
      Math.min(english.totalHits, articles.totalHits),
    );

    await filterValue(url).fill("*arabica*");
    const arabica = await runFilters(page);
    expect(arabica.totalHits).toBeGreaterThan(0);
    for (const value of storedValues(arabica, "Url")) {
      expect(value).toContain("arabica");
    }

    await page
      .getByRole("button", { name: "Remove filter 1: URL", exact: true })
      .click();
    const type = await addFilter(
      page,
      "Content type code name",
      "DancingGoat.*",
    );
    const prefixed = await runFilters(page);
    expect(prefixed.totalHits).toBeGreaterThan(0);
    for (const value of storedValues(prefixed, "ContentTypeName")) {
      expect(value.startsWith("DancingGoat.")).toBe(true);
    }

    await filterValue(type).fill("*ArticlePage*");
    const containing = await runFilters(page);
    expect(containing.totalHits).toBeGreaterThan(0);
    expect(containing.totalHits).toBeLessThanOrEqual(prefixed.totalHits);
    for (const value of storedValues(containing, "ContentTypeName")) {
      expect(value).toContain("ArticlePage");
    }

    // Keyword values are not analyzed, so case must match the indexed value.
    await filterValue(type).fill("dancinggoat.*");
    const lowercase = await runFilters(page);
    expect(lowercase.totalHits).toBe(0);
    await expect(page.getByTestId("search-results")).toHaveCount(0);
    await expect(page.getByTestId("search-empty")).toContainText(
      "No documents matched",
    );
    expect(commands.every((c) => readOnlyCommands.includes(c))).toBe(true);
  });

  test("analyzed text filters use the analyzer and stay bound to their field", async ({
    page,
  }) => {
    await openSearch(page);
    const title = await addFilter(page, "Title", "coffee");
    const words = await runFilters(page);
    expect(words.totalHits).toBeGreaterThan(0);
    for (const value of storedValues(words, "Title")) {
      expect(value).toMatch(/coffee/i);
    }

    await filterValue(title).fill("coff*");
    const prefix = await runFilters(page);
    await expect(page.getByTestId("search-parsed")).toContainText(
      "Title:coff*",
    );
    expect(prefix.totalHits).toBeGreaterThanOrEqual(words.totalHits);
    for (const value of storedValues(prefix, "Title")) {
      expect(value).toMatch(/\bcoff/i);
    }

    // Analyzed text is lowercased, unlike keyword values.
    await filterValue(title).fill("COFF*");
    const uppercase = await runFilters(page);
    expect(uppercase.totalHits).toBe(prefix.totalHits);

    // Query syntax in a value cannot target another field.
    for (const value of ["coffee OR Url:x", "coff* OR Url:*"]) {
      await filterValue(title).fill(value);
      const injected = await runFilters(page);
      const clauses = (injected.parsedQuery ?? "")
        .split(" ")
        .map((clause) => clause.replace(/^[+(]+/, ""));
      expect(clauses.length).toBeGreaterThan(0);
      for (const clause of clauses) {
        expect(clause.startsWith("Title:")).toBe(true);
      }
      await expect(page.getByTestId("search-parsed")).not.toContainText("Url:");
    }
  });

  test("boolean filters offer Any, Yes and No, and Clear returns to browsing", async ({
    page,
  }) => {
    const total = await openSearch(page);
    const secured = await addFilter(page, "Secured");
    await expect(filterValue(secured)).toHaveValue("Any");

    // Any does not filter, so searching lists every stored document.
    const browse = page.waitForRequest((r) => isCommand(r, "LoadDocuments"));
    await page.getByRole("button", { name: "Search", exact: true }).click();
    await browse;
    await expect(page.getByTestId("search-total")).toHaveText(
      `Showing 1–${String(pageSize)} of ${String(total)} stored documents.`,
    );

    await filterValue(secured).click();
    await chooseMenuItem(page, "Yes");
    const yes = await runFilters(page);
    expect(new Set(storedValues(yes, "IsSecured"))).toEqual(
      new Set(yes.totalHits === 0 ? [] : ["true"]),
    );

    await filterValue(secured).click();
    await chooseMenuItem(page, "No");
    const no = await runFilters(page);
    expect(no.totalHits).toBeGreaterThan(0);
    expect(new Set(storedValues(no, "IsSecured"))).toEqual(new Set(["false"]));
    expect(yes.totalHits + no.totalHits).toBe(total);

    const clear = page.getByRole("button", { name: "Clear", exact: true });
    await clear.click();
    await expect(filterRows(page)).toHaveCount(0);
    await expect(page.getByTestId("search-parsed")).toHaveCount(0);
    await expect(
      page.getByRole("heading", { level: 2, name: "Stored documents" }),
    ).toBeVisible();
    await expect(page.getByTestId("search-total")).toHaveText(
      `Showing 1–${String(pageSize)} of ${String(total)} stored documents.`,
    );
    await expect(clear).toBeDisabled();
  });

  test("filter rows keep the icon-only remove action beside the value at every width", async ({
    page,
  }) => {
    await page.setViewportSize({ width: 1600, height: 900 });
    await openSearch(page);
    await addFilter(page, "Language", "en");
    await addFilter(page, "Title", "coffee");
    const filters = page.getByTestId("search-filters");
    await expect(filters.getByText("Field", { exact: true })).toHaveCount(1);
    await expect(filters.getByText("Value", { exact: true })).toHaveCount(1);
    await expect(page.getByTestId("search-filter-context")).toHaveCount(1);

    // Field names must fit the picker without clipping.
    await filterRows(page).first().locator("input").first().click();
    const clipped = await page
      .getByTestId("menu-item")
      .evaluateAll(
        (items) =>
          items
            .flatMap((item) => [item, ...item.querySelectorAll("*")])
            .filter((e) => e.scrollWidth > e.clientWidth + 1).length,
      );
    expect(clipped).toBe(0);
    await page.getByRole("heading", { level: 1, name: "Search" }).click();
    await expect(page.getByTestId("menu-item")).toHaveCount(0);

    // Wide screens keep the field, value and remove action on one line; narrow screens wrap the value below the field.
    for (const { width, stacked } of [
      { width: 1600, stacked: false },
      { width: 700, stacked: true },
    ]) {
      await page.setViewportSize({ width, height: 900 });
      for (const [index, field] of ["Language", "Title"].entries()) {
        const row = filterRows(page).nth(index);
        const name = `Remove filter ${String(index + 1)}: ${field}`;
        const remove = row.getByRole("button", { name, exact: true });
        await expect(remove).toHaveText("");
        const fieldBox = await bounds(row.locator("input").first());
        const valueBox = await bounds(filterValue(row));
        const removeBox = await bounds(remove);
        const rowBox = await bounds(row);
        const removeCenter = removeBox.y + removeBox.height / 2;
        expect(removeCenter).toBeGreaterThan(valueBox.y);
        expect(removeCenter).toBeLessThan(valueBox.y + valueBox.height);
        expect(removeBox.x).toBeGreaterThanOrEqual(valueBox.x + valueBox.width);
        expect(removeBox.x + removeBox.width).toBeLessThanOrEqual(
          rowBox.x + rowBox.width + 1,
        );
        expect(valueBox.y >= fieldBox.y + fieldBox.height - 1).toBe(stacked);
      }
    }

    await page.setViewportSize({ width: 1600, height: 900 });
    const removeTitle = page.getByRole("button", {
      name: "Remove filter 2: Title",
      exact: true,
    });
    await removeTitle.hover();
    await expect(
      page.getByText("Remove filter 2: Title", { exact: true }),
    ).toBeVisible();
    await removeTitle.focus();
    await page.keyboard.press("Enter");
    await expect(filterRows(page)).toHaveCount(1);
    await expect(
      page.getByRole("group", { name: "Filter 1: Language", exact: true }),
    ).toBeVisible();
  });
});
