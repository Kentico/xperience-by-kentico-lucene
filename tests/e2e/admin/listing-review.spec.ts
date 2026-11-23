import { expect, test, type Locator, type Page } from "@playwright/test";
import { adminBaseUrl } from "../shared/config";
import { luceneIndexName } from "./luceneIndexesPage";

const listingUrl = `${adminBaseUrl}/lucene/indexes`;

async function openListing(page: Page): Promise<Locator> {
  await page.goto(listingUrl);
  const row = page.getByTestId("table-row").filter({
    has: page.getByRole("cell", { name: luceneIndexName, exact: true }),
  });
  await expect(row).toBeVisible({ timeout: 30_000 });
  return row;
}

async function cellText(page: Page, row: Locator, caption: string) {
  const captions = await page.getByRole("columnheader").allInnerTexts();
  const column = captions.findIndex((c) => c.trim() === caption);
  expect(column, `column ${caption}`).toBeGreaterThanOrEqual(0);
  return (await row.getByRole("cell").nth(column).innerText()).trim();
}

test.describe("Lucene index listing review", () => {
  test("uses native page title, sentence-case columns and a New index action", async ({
    page,
  }) => {
    await openListing(page);

    await expect(page.getByTestId("breadcrumb-current-tab")).toHaveText(
      "Indexes",
    );
    await expect(
      page.getByText("List of registered Lucene indices"),
    ).toHaveCount(0);
    const captions = (await page.getByRole("columnheader").allInnerTexts()).map(
      (c) => c.trim(),
    );
    expect(captions).toEqual([
      "ID",
      "Display name",
      "Code name",
      "Indexing strategy",
      "Analyzer",
      "Documents",
      "Last updated",
      "Actions",
    ]);

    await page.getByRole("link", { name: "New index", exact: true }).click();
    await expect(page).toHaveURL(/\/lucene\/indexes\/create$/);
  });

  test("shows real Lucene statistics instead of row placeholders", async ({
    page,
  }) => {
    const row = await openListing(page);

    const id = await cellText(page, row, "ID");
    const documents = await cellText(page, row, "Documents");
    const updated = await cellText(page, row, "Last updated");

    expect(documents).toMatch(/^\d+$/);
    expect(updated).not.toBe("Unavailable");
    expect(updated).not.toBe(id);
    expect(`${documents} ${updated}`).not.toMatch(/[0-9a-f]{8}-[0-9a-f]{4}-/i);
    expect(updated === "Never" || !Number.isNaN(Date.parse(updated))).toBe(
      true,
    );
    expect(await cellText(page, row, "Display name")).not.toBe("");
  });

  test("only database-backed columns are sortable", async ({ page }) => {
    await openListing(page);

    for (const caption of ["Documents", "Last updated"]) {
      const header = page.getByRole("columnheader", {
        name: caption,
        exact: true,
      });
      await expect(header).not.toHaveAttribute("tabindex", "0");
      await expect(header).not.toHaveClass(/sortable|sorted/);
      await header.getByTestId("xp-i-circle").hover();
      await expect(
        page.getByText(/on this application instance/),
      ).toBeVisible();
      await page.mouse.move(0, 0);
    }

    const codeName = page.getByRole("columnheader", {
      name: "Code name",
      exact: true,
    });
    await expect(codeName).toHaveAttribute("tabindex", "0");
    await expect(codeName).not.toHaveClass(/sorted/);
    await codeName.focus();
    await codeName.press("Enter");
    await expect(codeName).toHaveClass(/sorted/);
  });

  test("search filters by code name and shows the empty table state", async ({
    page,
  }) => {
    await openListing(page);
    const search = page.getByTestId("search-input");

    await search.fill(luceneIndexName.toLowerCase());
    await search.press("Enter");
    await expect(
      page.getByTestId("table-row").filter({ hasText: luceneIndexName }),
    ).not.toHaveCount(0);

    await search.fill(`no-such-index-${Date.now()}`);
    await search.press("Enter");
    await expect(page.getByTestId("table-row")).toHaveCount(0, {
      timeout: 15_000,
    });
  });

  test("row actions are titled and delete asks for an explicit confirmation", async ({
    page,
  }) => {
    const row = await openListing(page);
    await row.hover();

    const rebuild = row.getByTestId("button-Rebuild");
    const remove = row.getByTestId("button-Delete");
    await expect(rebuild).toBeVisible();
    await expect(remove).toBeVisible();
    await rebuild.hover();
    await expect(
      page.getByText("Rebuild index", { exact: true }),
    ).toBeVisible();

    await remove.focus();
    await remove.press("Enter");
    const dialog = page.getByRole("dialog", { name: "Delete index?" });
    await expect(dialog).toBeVisible();
    await expect(dialog).toContainText("cannot be undone");
    await dialog.getByTestId("cancel-action").click();
    await expect(dialog).toHaveCount(0);
    await expect(row).toBeVisible();
  });

  test("listing has no browser-default disclosure widgets", async ({
    page,
  }) => {
    await openListing(page);
    await expect(page.locator("details, summary")).toHaveCount(0);
  });
});
