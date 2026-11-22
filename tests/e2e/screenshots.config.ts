import { defineConfig } from "@playwright/test";
import {
  adminStorageStatePath,
  appBaseUrl,
  webServerCommand,
  webServerTimeoutMs,
} from "./shared/config";

export default defineConfig({
  testDir: ".",
  testIgnore: ["node_modules/**"],
  fullyParallel: false,
  workers: 1,
  retries: 0,
  reporter: [["list"]],
  timeout: 10 * 60_000,
  use: {
    baseURL: appBaseUrl,
    browserName: "chromium",
    viewport: { width: 1440, height: 900 },
    deviceScaleFactor: 1,
    colorScheme: "light",
    reducedMotion: "reduce",
    locale: "en-US",
    trace: "retain-on-failure",
  },
  projects: [
    {
      name: "admin-setup",
      testMatch: /[\\/]admin[\\/]auth\.setup\.ts/,
    },
    {
      name: "screenshots",
      testMatch: /[\\/]screenshots[\\/].*\.screenshots\.ts/,
      use: { storageState: adminStorageStatePath },
      dependencies: ["admin-setup"],
    },
  ],
  webServer: {
    command: webServerCommand,
    url: `${appBaseUrl}/admin`,
    timeout: webServerTimeoutMs,
    reuseExistingServer: true,
    stdout: "pipe",
  },
});
