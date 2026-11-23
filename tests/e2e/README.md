# End-to-end tests

Playwright tests cover the DancingGoat Lucene search experience and the Lucene administration pages.

## Requirements

- A licensed DancingGoat database initialized from the committed CI repository. See [Contributing Setup](../../docs/Contributing-Setup.md).
- Node.js 26 and Chromium (`npm run install:browsers`).
- For local admin tests, start the admin client in Proxy mode with `npm start` in `src/Kentico.Xperience.Lucene.Admin/Client`. CI uses the embedded Release client.

## Run

```powershell
cd tests/e2e
npm ci
npm run install:browsers
npm test
```

`npm run test:live-site` and `npm run test:admin` run one test project. The config starts DancingGoat unless it is already running. Tests wait for the startup index rebuild before checking search results.

Before a commit, run `npm run typecheck`, `npm run lint`, and `npm run format:check`. These checks do not need an Xperience license.

## Screenshots

`npm run screenshots` captures the Lucene index listing and DancingGoat search results into `images/screenshots/`. Captures use a fixed viewport, light theme, and reduced motion. Review any generated images before committing or changing README links; this tool does not generate screenshots during CI.

## CI

The `build_and_test` CI job runs static checks and the Release build/tests for every pull request. The `e2e` job needs the `XPERIENCE_LICENSE_KEY` secret and uses `scripts/Initialize-Database.ps1` to create the database and restore the sample's CI repository before running browser coverage. When the secret is unavailable, the job reports an explicit skip; this is not a runtime test pass.

## Configuration

| Variable                    | Default                                                                     |
| --------------------------- | --------------------------------------------------------------------------- |
| `DANCING_GOAT_BASE_URL`     | `applicationUrl` from `examples/DancingGoat/Properties/launchSettings.json` |
| `ADMIN_BASE_URL`            | `<base URL>/admin`                                                          |
| `E2E_WEB_SERVER_COMMAND`    | `dotnet run --project examples/DancingGoat`                                 |
| `E2E_WEB_SERVER_TIMEOUT_MS` | `180000`                                                                    |

Admin specs cover display-name navigation, immutable code names, grouped Configuration
editors, typed Search filters (wildcards, Boolean values and AND matching), advanced
queries, paged results and stored-record details. Overview coverage includes rebuild
confirmation, health tooltips, independently paged diagnostics and expired-run/retry
states. Wizard coverage checks preview draft preservation and complete Review values.
Content type selection is checked with 50+ available types. Wizard specs stop before
creating an index.
