import fs from "node:fs";
import path from "node:path";

// Runtime endpoints and commands have local defaults with environment overrides for CI and Aspire.

const dancingGoatPath = path.resolve(
  __dirname,
  "../../../examples/DancingGoat",
);

// Default: the DancingGoat launch profile URL, so the randomized port set by
// repository setup is picked up without duplicating it here.
function launchProfileUrl(): string {
  const launchSettings = JSON.parse(
    fs.readFileSync(
      path.join(dancingGoatPath, "Properties/launchSettings.json"),
      "utf8",
    ),
  ) as { profiles: { DancingGoat: { applicationUrl: string } } };

  const url = launchSettings.profiles.DancingGoat.applicationUrl;
  return url.split(";")[0];
}

export const appBaseUrl = (
  process.env.DANCING_GOAT_BASE_URL ?? launchProfileUrl()
).replace(/\/$/, "");

export const adminBaseUrl = process.env.ADMIN_BASE_URL ?? `${appBaseUrl}/admin`;

// The repository's "administrator" account: its password hash is committed in the CI repository
// (examples/DancingGoat/App_Data/CIRepository/@global/cms.user), which every database restores.
export const adminUsername = "administrator";
export const adminPassword = "Pass@12345";

// Storage state written by the `admin-setup` project (admin/auth.setup.ts) and
// reused by `admin-tests`, so admin specs start signed in.
export const adminStorageStatePath = path.resolve(
  __dirname,
  "../playwright/.auth/admin.json",
);
export const webServerCommand =
  process.env.E2E_WEB_SERVER_COMMAND ??
  `dotnet run --project "${dancingGoatPath}"`;
export const webServerTimeoutMs = Number.parseInt(
  process.env.E2E_WEB_SERVER_TIMEOUT_MS ?? "180000",
  10,
);
