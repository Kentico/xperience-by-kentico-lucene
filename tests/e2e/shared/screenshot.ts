import fs from "node:fs/promises";
import path from "node:path";
import type { Locator, Page } from "@playwright/test";

export const screenshotsPath = path.resolve(
  __dirname,
  "../../../images/screenshots",
);

export interface CaptureOptions {
  fullHeight?: number;
  cropHeight?: number;
  element?: Locator;
}

export async function capture(
  page: Page,
  name: string,
  options: CaptureOptions = {},
): Promise<void> {
  const viewport = page.viewportSize();
  if (!viewport) {
    throw new Error("The page has no viewport size.");
  }

  const top = new URL(page.url()).pathname.toLowerCase().startsWith("/admin")
    ? await adminBannerHeight(page)
    : 0;
  await page.mouse.move(viewport.width - 1, viewport.height - 1);

  let height = viewport.height;
  let fullPage = false;
  if (options.fullHeight) {
    const innerExtra = await scrollInnerContentToTop(page);
    if (innerExtra > 0) {
      height = Math.min(viewport.height + innerExtra, options.fullHeight);
      await page.setViewportSize({ width: viewport.width, height });
    } else {
      height = Math.min(
        await page.evaluate(() => document.documentElement.scrollHeight),
        options.fullHeight,
      );
      fullPage = true;
    }
  } else {
    await scrollInnerContentToTop(page);
  }

  try {
    await waitForRender(page);
    let clip = {
      x: 0,
      y: top,
      width: viewport.width,
      height: Math.min(height, options.cropHeight ?? height) - top,
    };

    if (options.element) {
      await options.element.scrollIntoViewIfNeeded();
      const box = await options.element.boundingBox();
      if (!box) {
        throw new Error(`The element for screenshot "${name}" is not visible.`);
      }
      const margin = 16;
      const x = Math.max(0, box.x - margin);
      const y = Math.max(top, box.y - margin);
      clip = {
        x,
        y,
        width: Math.min(box.x + box.width + margin, viewport.width) - x,
        height: Math.min(
          box.y + box.height + margin - y,
          options.cropHeight ?? Number.POSITIVE_INFINITY,
        ),
      };
    }

    await fs.mkdir(screenshotsPath, { recursive: true });
    await page.screenshot({
      path: path.join(screenshotsPath, `${name}.png`),
      animations: "disabled",
      caret: "hide",
      fullPage,
      clip,
    });
  } finally {
    if (height !== viewport.height && !fullPage) {
      await page.setViewportSize(viewport);
    }
  }
}

async function adminBannerHeight(page: Page): Promise<number> {
  return page.evaluate(() => {
    const banner = document.querySelector('[class^="banner___"]');
    const box = banner?.getBoundingClientRect();
    return box?.top === 0 ? Math.ceil(box.height) : 0;
  });
}

async function scrollInnerContentToTop(page: Page): Promise<number> {
  return page.evaluate(() => {
    let extra = 0;
    for (const element of Array.from(
      document.querySelectorAll<HTMLElement>("body *"),
    )) {
      const overflowY = getComputedStyle(element).overflowY;
      if (
        (overflowY === "auto" || overflowY === "scroll") &&
        element.scrollHeight > element.clientHeight + 1
      ) {
        element.scrollTop = 0;
        extra = Math.max(extra, element.scrollHeight - element.clientHeight);
      }
    }
    window.scrollTo(0, 0);
    return extra;
  });
}

async function waitForRender(page: Page): Promise<void> {
  await page.evaluate(async () => {
    await document.fonts.ready;
    for (const image of Array.from(document.images)) {
      image.loading = "eager";
    }
    await Promise.all(
      Array.from(document.images)
        .filter((image) => !image.complete)
        .map(
          (image) =>
            new Promise<void>((resolve) => {
              image.addEventListener(
                "load",
                () => {
                  resolve();
                },
                { once: true },
              );
              image.addEventListener(
                "error",
                () => {
                  resolve();
                },
                { once: true },
              );
            }),
        ),
    );
    for (let frame = 0; frame < 2; frame++) {
      await new Promise<void>((resolve) => {
        requestAnimationFrame(() => {
          resolve();
        });
      });
    }
  });
}
