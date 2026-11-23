import { expect, test } from "@playwright/test";
import { CreateWizard, checkboxLabel, redTextIn } from "./createWizard";

test.describe("Lucene create index wizard review", () => {
  test("source editor validates paths and types and Cancel discards drafts", async ({
    page,
  }) => {
    const wizard = new CreateWizard(page);
    await wizard.start(`Wizard.Paths.${Date.now()}`);
    await wizard.next();
    await expect(page.getByTestId("wizard-error")).toHaveText(
      "Add a channel with a path and content type, or select a reusable content type.",
    );

    await wizard.openNewChannel();
    await expect(wizard.panelButton("Preview path")).toBeDisabled();
    await wizard.panelButton("Add channel").click();
    await expect(wizard.form()).toContainText("Enter a path.");
    await expect(page.getByTestId("wizard-source-error")).toHaveText(
      "Select at least one content type.",
    );
    await wizard.fillPath("Articles/%", ["Article page"]);
    await expect(wizard.panelButton("Preview path")).toBeEnabled();
    await wizard.panelButton("Add channel").click();
    await expect(wizard.form()).toContainText(
      "Paths must start with '/', for example /Articles/%.",
    );
    await expect(page.getByTestId("wizard-source-error")).toHaveCount(0);
    await expect(wizard.channelBlocks()).toHaveCount(0);

    await wizard.pathInput().fill("/%");
    await wizard.apply("Add channel");
    const channel =
      (await wizard.channelBlocks().first().getAttribute("aria-label")) ?? "";
    expect(await wizard.pathValues()).toEqual(["/%"]);
    await expect(
      wizard.content().getByRole("button", {
        name: "Add website channel",
        exact: true,
      }),
    ).toBeVisible();

    await wizard
      .content()
      .getByRole("button", { name: `Add path to ${channel}`, exact: true })
      .click();
    await wizard.fillPath("/%", ["Article page"]);
    await wizard.panelButton("Add path").click();
    await expect(wizard.form()).toContainText(
      "This channel already includes this path.",
    );
    await wizard.panelButton("Cancel").click();
    await expect(wizard.form()).toHaveCount(0);
    expect(await wizard.pathValues()).toEqual(["/%"]);

    await wizard
      .content()
      .getByRole("button", { name: "Edit path /%", exact: true })
      .click();
    await expect(wizard.pathInput()).toHaveValue("/%");
    expect(
      await wizard.indexes.selectedContentTypes(wizard.typesSelector()),
    ).toEqual(["Article page"]);
    await wizard.pathInput().fill("/Articles/%");
    await wizard.panelButton("Cancel").click();
    expect(await wizard.pathValues()).toEqual(["/%"]);

    await wizard
      .content()
      .getByRole("button", { name: "Edit path /%", exact: true })
      .click();
    await wizard.pathInput().fill("/Articles/%");
    await wizard.apply("Apply");
    expect(await wizard.pathValues()).toEqual(["/Articles/%"]);
    await wizard.next();
    await expect(page.getByTestId("step-languages")).toBeVisible();
  });

  test("trashcan removals use contextual names and leave valid content", async ({
    page,
  }) => {
    const wizard = new CreateWizard(page);
    await wizard.start(`Wizard.Remove.${Date.now()}`);
    const channel = await wizard.addChannel("/Articles/%", ["Article page"]);
    await wizard.addPath(channel, "/Coffees/%", ["Article page"]);
    expect(await wizard.pathValues()).toEqual(["/Articles/%", "/Coffees/%"]);

    const removePath = wizard.content().getByRole("button", {
      name: "Remove path /Coffees/%",
      exact: true,
    });
    await expect(removePath).not.toContainText("Remove");
    await removePath.hover();
    await expect(page.locator("[data-tippy-root]")).toContainText(
      "Remove path /Coffees/%",
    );
    await removePath.focus();
    await removePath.press("Enter");
    expect(await wizard.pathValues()).toEqual(["/Articles/%"]);

    await wizard
      .content()
      .getByRole("button", { name: "Remove path /Articles/%", exact: true })
      .click();
    await expect(wizard.channelBlocks()).toContainText(
      "No paths. Add a path or remove the channel.",
    );
    await wizard.next();
    await expect(page.getByTestId("wizard-error").first()).toContainText(
      "Every channel needs at least one path",
    );

    const removeChannel = wizard.content().getByRole("button", {
      name: `Remove channel ${channel}`,
      exact: true,
    });
    await expect(removeChannel).not.toContainText("Remove");
    await removeChannel.click();
    await expect(wizard.channelBlocks()).toHaveCount(0);
    await expect(
      wizard.content().getByRole("button", {
        name: "Add website channel",
        exact: true,
      }),
    ).toBeEnabled();
  });

  test("closing the path preview restores the unsaved editor draft", async ({
    page,
  }) => {
    const wizard = new CreateWizard(page);
    await wizard.start(`Wizard.Preview.${Date.now()}`);
    await wizard.openNewChannel();
    const types = ["Article page", "Home page"];
    await wizard.fillPath("/%", types);
    await expect(
      wizard.panel().getByRole("heading", { name: "Add website channel" }),
    ).toBeVisible();

    await wizard.panelButton("Preview path").click();
    const preview = wizard.preview();
    await expect(wizard.form()).toBeHidden();
    await expect(page.getByTestId("path-preview-path")).toContainText("/%");
    await expect(page.getByTestId("path-preview-language")).toContainText(
      "default until languages are selected",
    );
    for (const type of types) {
      await expect(
        page.getByTestId("path-preview-content-types"),
      ).toContainText(type);
    }
    await expect(page.getByTestId("path-preview-summary")).toContainText(
      "Wildcard",
    );
    await expect(page.getByTestId("path-preview-summary")).toContainText(
      /[1-9]\d* matching pages/,
    );
    await expect(
      preview.getByTestId("path-preview-item").first(),
    ).toBeVisible();
    await expect(preview.getByRole("textbox")).toHaveCount(0);
    const channel = (
      await page.getByTestId("path-preview-channel").innerText()
    ).trim();
    expect(channel).not.toBe("");
    expect(await redTextIn(preview)).toEqual([]);

    await wizard.closePreview();
    await expect(wizard.form()).toBeVisible();
    await expect(wizard.pathInput()).toHaveValue("/%");
    expect(
      await wizard.indexes.selectedContentTypes(wizard.typesSelector()),
    ).toEqual(types);
    await expect(wizard.panelButton("Add channel")).toBeVisible();
    await expect(wizard.channelBlocks()).toHaveCount(0);

    const missing = `/wizard-no-page-${Date.now()}`;
    await wizard.pathInput().fill(missing);
    await wizard.panelButton("Preview path").click();
    await expect(page.getByTestId("path-preview-summary")).toContainText(
      "Exact path",
    );
    await expect(page.getByTestId("path-preview-summary")).toContainText(
      "0 matching pages",
    );
    await wizard.closePreview();
    await expect(wizard.pathInput()).toHaveValue(missing);
    expect(await redTextIn(wizard.form())).toEqual([]);

    await wizard.panelButton("Cancel").click();
    await expect(wizard.form()).toHaveCount(0);
    await expect(wizard.channelBlocks()).toHaveCount(0);
  });

  test("path previews from the summary display server errors", async ({
    page,
  }) => {
    await page.route("**/api/cmd", async (route) => {
      const command = /name="command"\r\n\r\n([^\r]+)/.exec(
        route.request().postData() ?? "",
      )?.[1];
      if (command !== "PreviewPath") {
        await route.fallback();
        return;
      }
      await route.fulfill({
        json: {
          commandName: null,
          messages: [],
          result: {
            total: 0,
            isWildcard: false,
            items: [],
            error: "The channel is no longer available.",
          },
        },
      });
    });

    const wizard = new CreateWizard(page);
    await wizard.start(`Wizard.PreviewError.${Date.now()}`);
    await wizard.addChannel("/%", ["Article page"]);
    await wizard
      .content()
      .getByRole("button", { name: "Preview path /%", exact: true })
      .click();
    await expect(page.getByTestId("path-preview-error")).toHaveText(
      "The channel is no longer available.",
    );
    await expect(page.getByTestId("path-preview-summary")).toHaveCount(0);
    await wizard.closePreview();
    await expect(wizard.form()).toHaveCount(0);
    expect(await wizard.pathValues()).toEqual(["/%"]);
  });

  test("review lists every path and type in full and steps can be revisited", async ({
    page,
  }) => {
    const wizard = new CreateWizard(page);
    const codeName = `Wizard.Review.${Date.now()}`;
    await wizard.indexes.openWizard();
    await expect(page.getByTestId("wizard")).not.toContainText(/Step \d of 5/);
    await page.getByLabel("Code name").fill(codeName);
    await expect(wizard.stepButton("2. Content")).toBeDisabled();
    await wizard.next();

    const longPath = `/Articles/${"long-segment-".repeat(8)}end/%`;
    const channel = await wizard.addChannel("/%", ["Article page"]);
    await wizard.addPath(channel, longPath, ["Article page", "Home page"]);
    await wizard.selectReusable(["Cafe"]);
    await expect(wizard.content()).not.toContainText(/selected ·|available/);

    await wizard.next();
    const language = await checkboxLabel(page, "Languages");
    expect(language).not.toBe("");
    await wizard.indexes.selectWizardCheckbox("Languages");
    await wizard.next();
    await wizard.next();
    await wizard.indexes.expectWizardStep("5. Review");

    const review = wizard.review();
    await expect(review).toContainText(codeName);
    await expect(review.getByTestId("wizard-review-channel")).toHaveCount(1);
    await expect(review.getByTestId("wizard-review-channel")).toContainText(
      channel,
    );
    const paths = review.getByTestId("wizard-review-path");
    await expect(paths).toHaveCount(2);
    await expect(paths.nth(0).locator("code")).toHaveText("/%");
    await expect(paths.nth(1).locator("code")).toHaveText(longPath);
    await expect(paths.nth(1).locator("code")).toBeInViewport({ ratio: 1 });
    await expect(paths.nth(0).getByTestId("wizard-type-name")).toHaveText([
      /^Article page \S+$/,
    ]);
    await expect(paths.nth(1).getByTestId("wizard-type-name")).toHaveText([
      /^Article page \S+$/,
      /^Home page \S+$/,
    ]);
    await expect(
      review
        .getByRole("list", { name: "Reusable content types" })
        .getByTestId("wizard-type-name"),
    ).toHaveText([/^Cafe \S+$/]);
    await expect(review).toContainText(language);
    await expect(review).not.toContainText("matching pages");
    await expect(page.getByTestId("step-review")).toContainText(
      "Rebuild it from Overview or the index list to fill it.",
    );
    expect(await redTextIn(review)).toEqual([]);
    expect(await redTextIn(page.getByTestId("wizard-steps"))).toEqual([]);

    await wizard.stepButton("2. Content").click();
    await wizard.indexes.expectWizardStep("2. Content");
    expect(await wizard.pathValues()).toEqual(["/%", longPath]);
    await wizard
      .content()
      .getByRole("button", { name: "Preview path /%", exact: true })
      .click();
    await expect(page.getByTestId("path-preview-language")).toContainText(
      "first selected language",
    );
    await wizard.closePreview();
    await wizard
      .content()
      .getByRole("button", { name: `Remove path ${longPath}`, exact: true })
      .click();
    await expect(wizard.stepButton("5. Review")).toBeEnabled();
    await wizard.stepButton("5. Review").click();
    await expect(paths).toHaveCount(1);

    await wizard.stepButton("2. Content").click();
    await wizard
      .content()
      .getByRole("button", { name: "Remove path /%", exact: true })
      .click();
    await wizard.stepButton("5. Review").click();
    await wizard.indexes.expectWizardStep("2. Content");
    await expect(page.getByTestId("wizard-error").first()).toContainText(
      "Every channel needs at least one path",
    );
  });

  test("reusable type selector searches, stays bounded and Cancel keeps applied types", async ({
    page,
  }) => {
    const wizard = new CreateWizard(page);
    await wizard.start(`Wizard.Types.${Date.now()}`);
    await wizard
      .content()
      .getByRole("button", { name: "Select reusable content types" })
      .click();

    const reusable = wizard.reusableSelector();
    const options = wizard.indexes.contentTypeOptions();
    const search = reusable.getByRole("textbox", {
      name: "Search Reusable content types",
    });
    await expect(reusable).not.toContainText(/selected ·|available/);
    await expect(options).toHaveCount(0);
    await search.click();
    await expect(options.first()).toBeVisible();
    expect(await options.count()).toBeGreaterThan(8);
    const bounds = await options.first().evaluate((el) => {
      let node: HTMLElement | null = el.parentElement;
      while (node && !/(auto|scroll)/.test(getComputedStyle(node).overflowY)) {
        node = node.parentElement;
      }
      return node
        ? { client: node.clientHeight, scroll: node.scrollHeight }
        : null;
    });
    expect(bounds).not.toBeNull();
    expect(bounds?.client).toBeLessThanOrEqual(272);
    expect(bounds?.scroll).toBeGreaterThan(bounds?.client ?? 0);
    await search.press("Escape");
    await expect(options).toHaveCount(0);
    await expect(wizard.form()).toBeVisible();

    await search.fill("zzz-no-type");
    await expect(
      page.getByText("No content types match 'zzz-no-type'."),
    ).toBeVisible();
    await search.fill("coffee");
    await expect(options).toHaveCount(1);
    await search.fill("");

    await wizard.indexes.selectContentType(reusable, "Coffee product");
    await wizard.indexes.selectContentType(reusable, "Cafe");
    expect(await wizard.indexes.selectedContentTypes(reusable)).toEqual([
      "Coffee product",
      "Cafe",
    ]);
    await wizard.indexes.removeContentType(reusable, "Cafe");
    await wizard.closeTypeMenu(reusable);
    await wizard.apply("Apply");
    const section = page.getByTestId("reusable-content-section");
    await expect(section.getByTestId("wizard-type-name")).toHaveText([
      /^Coffee product \S+$/,
    ]);

    await wizard
      .content()
      .getByRole("button", { name: "Edit reusable content types" })
      .click();
    await wizard.indexes.removeContentType(reusable, "Coffee product");
    await wizard.panelButton("Cancel").click();
    await expect(section.getByTestId("wizard-type-name")).toHaveText([
      /^Coffee product \S+$/,
    ]);

    await wizard.toReview();
    await expect(
      wizard
        .review()
        .getByRole("list", { name: "Reusable content types" })
        .getByTestId("wizard-type-name"),
    ).toHaveText([/^Coffee product \S+$/]);
  });
});
