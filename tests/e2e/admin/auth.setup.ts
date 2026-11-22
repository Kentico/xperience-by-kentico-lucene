import fs from "node:fs/promises";
import path from "node:path";
import { test } from "@playwright/test";
import { adminStorageStatePath } from "../shared/config";
import { signInToAdmin } from "./auth";

test("authenticate admin and persist storage state", async ({ page }) => {
  await signInToAdmin(page);

  await fs.mkdir(path.dirname(adminStorageStatePath), { recursive: true });
  await page.context().storageState({ path: adminStorageStatePath });
});
