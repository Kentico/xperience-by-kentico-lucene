import { expect, type Locator, type Page } from "@playwright/test";
import { adminBaseUrl, adminPassword, adminUsername } from "../shared/config";

const usernameSelectors = [
  'input[name="username"]',
  'input[name="userName"]',
  'input[type="email"]',
  '[data-testid="username"] input',
];

const passwordSelectors = [
  'input[name="password"]',
  'input[type="password"]',
  '[data-testid="password"] input',
];

const submitSelectors = [
  'button[type="submit"]',
  'button:has-text("Sign in")',
  'button:has-text("Log in")',
  '[data-testid="signInButton"]',
];

export const adminShellSelectors = [
  'a[aria-label="System"]',
  'a[aria-label="Content hub"]',
  'a[aria-label="Settings"]',
];

export async function signInToAdmin(page: Page): Promise<void> {
  await page.goto(adminBaseUrl);

  if (await isVisibleWithin(page, adminShellSelectors, 2_000)) {
    return;
  }

  const usernameInput = await waitForAnyVisible(
    page,
    usernameSelectors,
    30_000,
  );
  const passwordInput = await waitForAnyVisible(
    page,
    passwordSelectors,
    30_000,
  );

  await usernameInput.fill(adminUsername);
  await passwordInput.fill(adminPassword);
  await (await waitForAnyVisible(page, submitSelectors, 5_000)).click();

  await expect
    .poll(
      async () => {
        if (await isVisibleWithin(page, adminShellSelectors, 1_000)) {
          return "ready";
        }
        return new URL(page.url()).pathname.toLowerCase().includes("sign")
          ? "sign-in"
          : "loading";
      },
      { timeout: 60_000, message: "Admin shell did not render after sign-in." },
    )
    .toBe("ready");
}

async function isVisibleWithin(
  page: Page,
  selectors: string[],
  timeoutMs: number,
): Promise<boolean> {
  try {
    await waitForAnyVisible(page, selectors, timeoutMs);
    return true;
  } catch {
    return false;
  }
}

async function waitForAnyVisible(
  page: Page,
  selectors: string[],
  timeoutMs: number,
): Promise<Locator> {
  const locator = selectors
    .map((selector) => page.locator(selector))
    .reduce((any, next) => any.or(next))
    .filter({ visible: true })
    .first();

  await locator.waitFor({ state: "visible", timeout: timeoutMs });
  return locator;
}
