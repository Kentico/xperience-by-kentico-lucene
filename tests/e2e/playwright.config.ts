import { defineConfig, devices } from "@playwright/test";
import {
  adminStorageStatePath,
  appBaseUrl,
  webServerCommand,
  webServerTimeoutMs,
} from "./shared/config";

const isCi = Boolean(process.env.CI);

export default defineConfig({
  testDir: ".",
  testIgnore: ["node_modules/**"],
  fullyParallel: true,
  forbidOnly: isCi,
  retries: isCi ? 2 : 0,
  workers: isCi ? 1 : undefined,
  reporter: [["list"], ["html", { open: "never" }]],
  use: {
    baseURL: appBaseUrl,
    trace: "retain-on-failure",
    screenshot: "only-on-failure",
  },
  projects: [
    {
      name: "live-site",
      use: { ...devices["Desktop Chrome"] },
      testMatch: /[\\/]live-site[\\/].*\.spec\.ts/,
    },
    {
      name: "admin-setup",
      use: { ...devices["Desktop Chrome"] },
      testMatch: /[\\/]admin[\\/]auth\.setup\.ts/,
    },
    {
      name: "admin-tests",
      use: {
        ...devices["Desktop Chrome"],
        storageState: adminStorageStatePath,
      },
      testMatch: /[\\/]admin[\\/].*\.spec\.ts/,
      dependencies: ["admin-setup"],
    },
  ],
  webServer: {
    command: webServerCommand,
    url: `${appBaseUrl}/admin`,
    timeout: webServerTimeoutMs,
    reuseExistingServer: !isCi,
    stdout: "pipe",
  },
});
