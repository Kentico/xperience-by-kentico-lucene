import { expect, test, type Page } from "@playwright/test";
import { LuceneIndexesPage, luceneIndexName } from "./luceneIndexesPage";

interface Option {
  value: string;
  label: string;
}

const savedToast = /^Index edited\. Saving does not rebuild the index\./;

// Rewrites the webContentTypes client property returned by the admin page API.
async function overrideWebContentTypes(
  page: Page,
  transform: (items: Option[]) => Option[],
): Promise<void> {
  const walk = (node: unknown): void => {
    if (Array.isArray(node)) {
      node.forEach(walk);
    } else if (node && typeof node === "object") {
      const record = node as Record<string, unknown>;
      for (const [key, child] of Object.entries(record)) {
        if (key === "webContentTypes" && Array.isArray(child)) {
          record[key] = transform(child as Option[]);
        } else {
          walk(child);
        }
      }
    }
  };
  await page.route("**/admin/api/page**", async (route) => {
    const response = await route.fetch();
    const body = await response.text();
    if (!body.includes("webContentTypes")) {
      await route.fulfill({ response });
      return;
    }
    const json: unknown = JSON.parse(body);
    walk(json);
    await route.fulfill({ response, json });
  });
  await page.reload();
  await page.getByTestId("config-channel").first().waitFor();
}

const fixtureTypes: Option[] = Array.from({ length: 60 }, (_, i) => {
  const n = String(i + 1).padStart(2, "0");
  return { value: `Fixture.Type${n}`, label: `Fixture type ${n}` };
});

test.describe("Lucene index configuration", () => {
  test.beforeEach(async ({ page }) => {
    page.on("dialog", (dialog) => void dialog.accept());
    const indexes = new LuceneIndexesPage(page);
    await indexes.goto();
    await indexes.expectIndexListed();
    await indexes.openIndex();
  });

  test.afterEach(async ({ page }) => {
    await page.unrouteAll({ behavior: "ignoreErrors" });
  });

  test("shows the complete configuration read-only", async ({
    page,
  }, testInfo) => {
    const indexes = new LuceneIndexesPage(page);
    const root = page.getByTestId("index-configuration");

    const settings = page.getByTestId("config-settings");
    for (const text of [
      `Code name${luceneIndexName}`,
      "Display name",
      "Indexing strategyDancingGoatGlobalSearch",
      "Lucene analyzerStandard",
      "Rebuild hook",
    ]) {
      await expect(settings).toContainText(text);
    }
    const english = page
      .getByTestId("config-languages")
      .getByRole("list", { name: "Indexed languages" })
      .getByTestId("config-named-value")
      .filter({ hasText: "English" });
    await expect(english.getByTestId("config-value-name")).toHaveText(
      "English",
    );
    await expect(english.getByTestId("config-value-code")).toHaveText(/^en/);
    const channel = indexes.configuredChannel();
    await expect(channel).toContainText("Dancing Goat Pages");
    const paths = await indexes.pathValues();
    expect(paths.length).toBeGreaterThan(0);
    const article = channel
      .getByRole("list", { name: /^Content types for / })
      .getByTestId("config-named-value")
      .filter({ hasText: "Article page" })
      .first();
    await expect(article.getByTestId("config-value-name")).toHaveText(
      "Article page",
    );
    await expect(article.getByTestId("config-value-code")).toHaveText(
      "DancingGoat.ArticlePage",
    );
    const firstPath = channel.getByTestId("config-path").first();
    await expect(firstPath).toHaveText(
      new RegExp(
        `^Path${paths[0].replace(/[.*+?^${}()|[\]\\/]/g, "\\$&")}Content types\\S`,
      ),
    );
    await expect(firstPath).not.toContainText("-");
    await expect(
      page
        .getByTestId("config-reusable")
        .getByRole("list", { name: "Reusable content types" })
        .getByRole("listitem")
        .first(),
    ).toBeVisible();

    for (const role of ["textbox", "combobox", "checkbox", "option"] as const) {
      await expect(root.getByRole(role)).toHaveCount(0);
    }
    await expect(root.locator("[aria-expanded]")).toHaveCount(0);
    await expect(root.locator("[class*='tag___']")).toHaveCount(0);
    await expect(indexes.configPanel()).toHaveCount(0);

    // Custom text must use the native text colors, not the inherited admin red.
    const colors = await root.evaluate((el) => {
      const probe = (value: string): string => {
        const span = document.createElement("span");
        span.style.color = value;
        el.appendChild(span);
        const color = getComputedStyle(span).color;
        span.remove();
        return color;
      };
      const item = el.querySelector("[aria-label='Indexed languages'] li");
      const spans = item ? Array.from(item.querySelectorAll("span")) : [];
      return {
        primary: spans[0] ? getComputedStyle(spans[0]).color : "",
        secondary: spans[1] ? getComputedStyle(spans[1]).color : "",
        expectedPrimary: probe("var(--color-text-default-on-light)"),
        expectedSecondary: probe("var(--color-text-low-emphasis)"),
      };
    });
    expect(colors.primary).toBe(colors.expectedPrimary);
    expect(colors.secondary).toBe(colors.expectedSecondary);
    expect(colors.primary).not.toBe(colors.expectedSecondary);
    for (const name of [
      "Edit index settings",
      "Edit languages",
      "Edit Dancing Goat Pages",
      "Edit reusable content",
    ]) {
      await expect(
        page.getByRole("button", { name, exact: true }),
      ).toBeEnabled();
    }
    for (const name of [
      "Edit identity",
      "Edit indexing",
      "Edit rebuild hook",
    ]) {
      await expect(page.getByRole("button", { name })).toHaveCount(0);
    }

    await testInfo.attach("Configuration", {
      body: await page.screenshot({
        path: testInfo.outputPath("configuration.png"),
        fullPage: true,
      }),
      contentType: "image/png",
    });
  });

  test("index settings share one panel; other objects have their own", async ({
    page,
  }) => {
    const indexes = new LuceneIndexesPage(page);

    const settings = await indexes.openSectionEditor("Edit index settings");
    await expect(indexes.configPanel()).toHaveCount(1);
    for (const name of ["Display name", "Rebuild hook"]) {
      await expect(settings.getByRole("textbox", { name })).toBeVisible();
    }
    await expect(settings.getByText("Indexing strategy").first()).toBeVisible();
    await expect(settings.getByText("Lucene analyzer").first()).toBeVisible();
    const codeName = settings.getByTestId("config-code-name");
    await expect(codeName).toHaveText(
      `Code name (cannot be changed)${luceneIndexName}`,
    );
    await expect(
      settings.getByRole("textbox", { name: /Code name/ }),
    ).toHaveCount(0);
    // The immutable code name is secondary text: smaller than its native label.
    const sizes = await codeName.evaluate((el) => {
      const size = (node: Element | null): number =>
        node ? parseFloat(getComputedStyle(node).fontSize) : 0;
      return {
        label: size(el.firstElementChild),
        value: size(el.querySelector("[data-testid=config-value-code]")),
      };
    });
    expect(sizes.value).toBeGreaterThan(0);
    expect(sizes.value).toBeLessThan(sizes.label);
    await expect(settings.getByRole("checkbox")).toHaveCount(0);
    await page.getByRole("button", { name: "Cancel", exact: true }).click();
    await expect(indexes.configPanel()).toHaveCount(0);

    const languages = await indexes.openSectionEditor("Edit languages");
    await expect(languages.getByRole("checkbox")).not.toHaveCount(0);
    await expect(languages.getByRole("textbox")).toHaveCount(0);
    await page.getByRole("button", { name: "Cancel", exact: true }).click();
    await expect(indexes.configPanel()).toHaveCount(0);

    const channel = await indexes.openChannelEditor();
    await expect(indexes.configPanel()).toHaveCount(1);
    await expect(indexes.panelPaths()).toHaveCount(
      (await indexes.pathValues()).length,
    );
    await expect(channel.getByRole("checkbox")).toHaveCount(0);
    await expect(
      channel.getByRole("textbox", { name: "Display name" }),
    ).toHaveCount(0);
    await expect(indexes.contentTypeMenu()).toHaveCount(0);
  });

  test("cancel discards all index settings together", async ({ page }) => {
    const indexes = new LuceneIndexesPage(page);
    const settings = page.getByTestId("config-settings");
    const before = (await settings.textContent()) ?? "";

    const panel = await indexes.openSectionEditor("Edit index settings");
    await panel
      .getByRole("textbox", { name: "Display name" })
      .fill("Unsaved display name");
    await panel
      .getByRole("textbox", { name: "Rebuild hook" })
      .fill("unsaved-hook");
    await page.getByRole("button", { name: "Cancel", exact: true }).click();
    await expect(indexes.configPanel()).toHaveCount(0);
    await expect(settings).toHaveText(before);
    await expect(page.getByText(savedToast)).toHaveCount(0);

    await page.reload();
    await expect(settings).toHaveText(before);
  });

  test("saves index settings together and shows the toast", async ({
    page,
  }) => {
    const indexes = new LuceneIndexesPage(page);
    const settings = page.getByTestId("config-settings");
    const panel = await indexes.openSectionEditor("Edit index settings");
    const displayName = panel.getByRole("textbox", { name: "Display name" });
    const rebuildHook = panel.getByRole("textbox", { name: "Rebuild hook" });
    const original = {
      displayName: await displayName.inputValue(),
      rebuildHook: await rebuildHook.inputValue(),
    };
    const stamp = String(Date.now());

    await displayName.fill(`Playwright display ${stamp}`);
    await rebuildHook.fill(`playwright-hook-${stamp}`);
    try {
      await indexes.saveIndexConfiguration();
      await page.reload();
      await expect(settings).toContainText(`Playwright display ${stamp}`);
      await expect(settings).toContainText(`playwright-hook-${stamp}`);
      await expect(settings).toContainText(`Code name${luceneIndexName}`);
      // The saved display name, not the code name, names the index in navigation.
      await expect(
        page.getByTestId("breadcrumbs").getByRole("link", {
          name: `Playwright display ${stamp}`,
          exact: true,
        }),
      ).toBeVisible();
      await expect(
        page.getByTestId("side-navigation-vertical-menu"),
      ).toContainText(`Playwright display ${stamp}`);
    } finally {
      const restore = await indexes.openSectionEditor("Edit index settings");
      await restore
        .getByRole("textbox", { name: "Display name" })
        .fill(original.displayName);
      await restore
        .getByRole("textbox", { name: "Rebuild hook" })
        .fill(original.rebuildHook);
      await indexes.saveIndexConfiguration();
    }
    await expect(settings).not.toContainText(stamp);
  });

  test("breadcrumbs and navigation show the display name", async ({ page }) => {
    const indexes = new LuceneIndexesPage(page);
    const panel = await indexes.openSectionEditor("Edit index settings");
    const displayName = (
      await panel.getByRole("textbox", { name: "Display name" }).inputValue()
    ).trim();
    await page.getByRole("button", { name: "Cancel", exact: true }).click();
    await expect(indexes.configPanel()).toHaveCount(0);
    expect(displayName).not.toBe("");
    expect(displayName).not.toBe(luceneIndexName);
    const crumbs = page.getByTestId("breadcrumbs");
    await expect(
      crumbs.getByRole("link", { name: displayName, exact: true }),
    ).toBeVisible();
    await expect(crumbs).not.toContainText(luceneIndexName);
    const nav = page.getByTestId("side-navigation-vertical-menu");
    await expect(nav).toContainText(displayName);
    await expect(nav.getByText(luceneIndexName, { exact: true })).toHaveCount(
      0,
    );
  });

  test("cancelling channel edits keeps every saved path and type", async ({
    page,
  }) => {
    const indexes = new LuceneIndexesPage(page);
    const channel = indexes.configuredChannel();
    const readOnly = (await channel.textContent()) ?? "";
    const paths = await indexes.pathValues();
    const savedTypes = await channel
      .getByTestId("config-path")
      .evaluateAll((items) =>
        items.map((item) =>
          Array.from(
            item.querySelectorAll("[data-testid=config-value-name]"),
          ).map((name) => name.textContent.trim()),
        ),
      );

    // The editor starts from exactly the saved paths and types.
    await indexes.openChannelEditor();
    expect(await indexes.panelPathValues()).toEqual(paths);
    for (let i = 0; i < paths.length; i++) {
      const selector = indexes
        .panelPaths()
        .nth(i)
        .getByTestId("path-content-types");
      expect(await indexes.selectedContentTypes(selector)).toEqual(
        savedTypes[i],
      );
    }

    // Removing one path only removes that path from the draft.
    const remove = indexes
      .panelPaths()
      .first()
      .getByRole("button", { name: `Remove included path ${paths[0]}` });
    await remove.focus();
    await page.keyboard.press("Enter");
    await expect.poll(() => indexes.panelPathValues()).toEqual(paths.slice(1));
    await page
      .getByRole("button", { name: "Add included path", exact: true })
      .click();
    await indexes
      .panelPaths()
      .last()
      .getByRole("textbox", { name: "Path" })
      .fill("/Unsaved/%");
    await expect(channel).toHaveText(readOnly);

    await page.getByRole("button", { name: "Cancel", exact: true }).click();
    await expect(indexes.configPanel()).toHaveCount(0);
    await expect(channel).toHaveText(readOnly);
    await expect(page.getByText(savedToast)).toHaveCount(0);

    await indexes.openChannelEditor();
    expect(await indexes.panelPathValues()).toEqual(paths);
    await page.getByRole("button", { name: "Cancel", exact: true }).click();
    await page.reload();
    await expect(channel).toHaveText(readOnly);
  });

  test("channel panel groups each path and separates channel removal", async ({
    page,
  }) => {
    const indexes = new LuceneIndexesPage(page);
    const panel = await indexes.openChannelEditor();
    await expect(
      page.getByRole("region", { name: "Edit website channel" }),
    ).toBeVisible();
    const identity = panel.getByTestId("config-panel-channel");
    await expect(identity).toContainText("Dancing Goat Pages");
    await expect(identity).toContainText("DancingGoatPages");
    await expect(
      identity.getByRole("button", {
        name: "Remove Dancing Goat Pages from index",
      }),
    ).toBeVisible();
    await expect(page.getByRole("button", { name: /from index$/ })).toHaveCount(
      1,
    );
    await expect(panel.getByText(/Included path \d/)).toHaveCount(0);

    const paths = indexes.panelPaths();
    const count = await paths.count();
    for (let i = 0; i < count; i++) {
      const group = paths.nth(i);
      const value = await group
        .getByRole("textbox", { name: "Path" })
        .inputValue();
      await expect(group).toHaveAttribute(
        "aria-label",
        `Included path ${value}`,
      );
      await expect(group.getByRole("textbox", { name: "Path" })).toHaveCount(1);
      await expect(
        group.getByRole("group", { name: "Content types", exact: true }),
      ).toHaveCount(1);
      // Remove actions are icon-only; the contextual name comes from the hover title.
      const remove = group.getByRole("button", {
        name: `Remove included path ${value}`,
        exact: true,
      });
      await expect(remove).toHaveCount(1);
      await expect(remove).toHaveText("");
      await expect(group.getByRole("button", { name: /^Remove / })).toHaveCount(
        1,
      );
    }
    await expect(
      paths.getByRole("button", { name: "Add included path" }),
    ).toHaveCount(0);
    await page
      .getByRole("button", { name: "Add included path", exact: true })
      .click();
    await expect(paths).toHaveCount(count + 1);
    await expect(paths.last()).toHaveAttribute(
      "aria-label",
      "New included path",
    );
    await page.getByRole("button", { name: "Cancel", exact: true }).click();
    await expect(indexes.configPanel()).toHaveCount(0);
  });

  test("rejects an invalid channel without saving", async ({ page }) => {
    const indexes = new LuceneIndexesPage(page);
    const original = await indexes.pathValues();

    const panel = await indexes.openChannelEditor();
    await indexes
      .panelPaths()
      .first()
      .getByRole("textbox", { name: "Path" })
      .fill("");
    await indexes.saveButton().click();
    await expect(panel.getByTestId("config-panel-errors")).toBeVisible();
    await expect(page.getByText(savedToast)).toHaveCount(0);
    await expect(indexes.configPanel()).toHaveCount(1);

    await page.getByRole("button", { name: "Cancel", exact: true }).click();
    expect(await indexes.pathValues()).toEqual(original);
    await page.reload();
    expect(await indexes.pathValues()).toEqual(original);
  });

  test("searches 60+ content types with mouse and keyboard", async ({
    page,
  }) => {
    const indexes = new LuceneIndexesPage(page);
    await overrideWebContentTypes(page, (items) => [...items, ...fixtureTypes]);
    const readOnly =
      (await indexes
        .configuredChannel()
        .getByTestId("config-path")
        .first()
        .textContent()) ?? "";

    await indexes.openChannelEditor();
    const block = indexes.panelPaths().first();
    const selector = block.getByTestId("path-content-types");
    const original = await indexes.selectedContentTypes(selector);
    const options = indexes.contentTypeOptions();
    const search = selector.getByRole("textbox", { name: /^Search / });
    const addPath = page.getByRole("button", { name: "Add included path" });

    await expect(indexes.contentTypeMenu()).toHaveCount(0);
    await addPath.click({ trial: true });
    const before = await addPath.boundingBox();
    await search.click();
    await expect.poll(() => options.count()).toBeGreaterThanOrEqual(60);
    expect(await addPath.boundingBox()).toEqual(before);
    const menuBox = await indexes.contentTypeMenu().boundingBox();
    expect(menuBox?.height ?? 0).toBeLessThanOrEqual(300);
    await expect(options.last()).not.toBeInViewport();

    await search.fill("type 42");
    await expect(options).toHaveCount(1);
    await search.fill("no such type");
    await expect(
      indexes.contentTypeMenu().getByText(/No content types match/),
    ).toBeVisible();

    await search.fill("Fixture type 4");
    await page.keyboard.press("ArrowDown");
    const option = indexes.contentTypeOption(selector, "Fixture type 40");
    await expect(option.locator(":scope > *")).toBeFocused();
    await page.keyboard.press("Space");
    await expect(option).toHaveAttribute("data-selected", "true");
    await page.keyboard.press("Escape");
    await expect(indexes.contentTypeMenu()).toHaveCount(0);
    await expect(indexes.configPanel()).toBeVisible();
    await expect(search).toBeFocused();

    await indexes.selectContentType(selector, "Fixture type 07");
    // Measure relative to the path block so panel scrolling does not count as a shift.
    const offset = async (): Promise<number> =>
      ((await addPath.boundingBox())?.y ?? 0) -
      ((await block.boundingBox())?.y ?? 0);
    const selected = await offset();
    await block.getByRole("textbox", { name: "Path" }).click();
    await expect(indexes.contentTypeMenu()).toHaveCount(0);
    expect(await offset()).toBeCloseTo(selected, 0);
    await indexes.selectContentType(selector, "Fixture type 58");
    await indexes.removeContentType(selector, "Fixture type 07");
    expect(await indexes.selectedContentTypes(selector)).toEqual([
      ...original,
      "Fixture type 40",
      "Fixture type 58",
    ]);

    await page.getByRole("button", { name: "Cancel", exact: true }).click();
    await expect(indexes.configPanel()).toHaveCount(0);
    await expect(
      indexes.configuredChannel().getByTestId("config-path").first(),
    ).toHaveText(readOnly);
  });

  test("clicks below an open selector reach their target", async ({ page }) => {
    const indexes = new LuceneIndexesPage(page);
    await indexes.openChannelEditor();
    const count = await indexes.panelPaths().count();
    await indexes
      .panelPaths()
      .first()
      .getByRole("textbox", { name: /^Search / })
      .click();
    await expect(indexes.contentTypeMenu()).toBeVisible();
    await page.keyboard.press("Escape");
    await page
      .getByRole("button", { name: "Add included path", exact: true })
      .click();
    await expect(indexes.panelPaths()).toHaveCount(count + 1);
    await page.getByRole("button", { name: "Cancel", exact: true }).click();
  });

  test("keeps selected content types that are no longer available", async ({
    page,
  }) => {
    const indexes = new LuceneIndexesPage(page);
    const channel = indexes.configuredChannel();
    const item = channel
      .getByRole("list", { name: /^Content types for / })
      .first()
      .getByTestId("config-named-value")
      .first();
    const first = (
      await item.getByTestId("config-value-name").innerText()
    ).trim();
    const code = (
      await item.getByTestId("config-value-code").innerText()
    ).trim();
    await overrideWebContentTypes(page, (items) =>
      items.filter((t) => t.label !== first),
    );

    const unavailable = `${first} (unavailable)`;
    // Read-only lists fall back to the code name; they do not invent a display name.
    const missing = channel
      .getByTestId("config-named-value")
      .filter({ hasText: code });
    await expect(missing.getByTestId("config-value-name")).toHaveText(code);
    await expect(missing.getByTestId("config-value-code")).toHaveText(
      "unavailable",
    );
    await indexes.openChannelEditor();
    const selector = indexes
      .panelPaths()
      .first()
      .getByTestId("path-content-types");
    expect(await indexes.selectedContentTypes(selector)).toContain(unavailable);
    await selector.getByRole("textbox", { name: /^Search / }).click();
    await expect(indexes.contentTypeOption(selector, first)).toHaveCount(0);
    await page.getByRole("button", { name: "Cancel", exact: true }).click();
    await expect(missing.getByTestId("config-value-code")).toHaveText(
      "unavailable",
    );
  });

  test("saves a content type selection", async ({ page }) => {
    const indexes = new LuceneIndexesPage(page);
    const path = () =>
      indexes.configuredChannel().getByTestId("config-path").first();
    const before = (await path().textContent()) ?? "";

    await indexes.openChannelEditor();
    const selector = () =>
      indexes.panelPaths().first().getByTestId("path-content-types");
    await selector()
      .getByRole("textbox", { name: /^Search / })
      .click();
    const extra = (
      await indexes
        .contentTypeMenu()
        .locator("[data-testid=content-type-option][data-selected=false]")
        .first()
        .innerText()
    )
      .split("\n")[0]
      .trim();
    await indexes.selectContentType(selector(), extra);
    await indexes.saveIndexConfiguration();
    await expect(path()).toContainText(extra);
    await page.reload();
    try {
      await expect(path()).toContainText(extra);
    } finally {
      await indexes.openChannelEditor();
      await indexes.restoreContentTypeRemoved(selector(), extra);
    }
    await page.reload();
    await expect(path()).toHaveText(before);
  });
});
