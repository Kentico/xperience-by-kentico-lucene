# Contributing Setup

## Required Software

The requirements to setup, develop, and build this project are listed below.

### .NET SDK

.NET SDK 11.0.100

- <https://dotnet.microsoft.com/en-us/download/dotnet/11.0>
- See the `global.json` file for specific SDK requirements
- The current library targets .NET 10; SDK 11 is the repository's build toolchain.

### Node.js Runtime

- [Node.js](https://nodejs.org/en/download) 26 (`>=26`)
- [NVM for Windows](https://github.com/coreybutler/nvm-windows) to manage multiple installed versions of Node.js
- See `engines` in `src/Kentico.Xperience.Lucene.Admin/Client/package.json` for specific version requirements

### Minimum npm package age

The client and end-to-end test `.npmrc` files require packages to be at least three days old. Dependabot uses the same default cooldown in `.github/dependabot.yml`. This delay reduces exposure to compromised or broken new releases.

### C# Editor

- VS Code
- Visual Studio
- Rider

### Database

SQL Server 2019 or newer compatible database

- [SQL Server Linux](https://learn.microsoft.com/en-us/sql/linux/sql-server-linux-setup)
- [SQL Server in Docker](https://learn.microsoft.com/en-us/sql/linux/quickstart-install-connect-docker)
- Docker is only needed when using the optional container-managed SQL Server or Mailpit services.

### SQL Editor

- MS SQL Server Management Studio
- VS Code with the [SQL Server (mssql)](https://marketplace.visualstudio.com/items?itemName=ms-mssql.mssql) extension

## Example Project

See the [DancingGoat README](../examples/DancingGoat/README.md) for full setup steps.

### Database Setup

Running the example project requires a licensed Xperience by Kentico database. Build DancingGoat, then run the setup script from the repository root:

```powershell
dotnet build
./scripts/Initialize-Database.ps1 -LicenseFile "<license_file_path>"
```

The script uses the sample's configured SQL Server and database by default, creates the database, and restores `examples/DancingGoat/App_Data/CIRepository`. See `./scripts/Initialize-Database.ps1 -?` for SQL Server, authentication, and database options. It does not replace an existing database unless `-Recreate` is supplied.

The `administrator` account uses the password `Pass@12345` stored in the sample's CI repository. Without a license file, add the license when you first sign in to the administration.

### Local services and Aspire

The sample uses a local SQL Server and Mailpit by default. Mailpit listens on `localhost:1025` for SMTP and provides an inbox at <http://localhost:8025>. Start Mailpit with Docker if it is not already running:

```powershell
docker run -d --name mailpit --restart unless-stopped -p 1025:1025 -p 8025:8025 axllent/mailpit
```

The Aspire AppHost starts DancingGoat and the admin client development server together. Install the [Aspire CLI](https://aspire.dev/get-started/install-cli/) and run this from the repository root:

```powershell
aspire run
```

By default, the AppHost uses the existing local SQL Server and Mailpit services. Container-managed SQL Server or Mailpit can be enabled explicitly in the AppHost user secrets; see `examples/AppHost` and the [DancingGoat README](../examples/DancingGoat/README.md). Set `XPERIENCE_LICENSE_FILE` to install a license when creating a container database.

### Admin Customization

The example project runs the Admin customization in development mode. `examples/DancingGoat/appsettings.Development.json` already contains the proxy configuration:

```json
"CMSAdminClientModuleSettings": {
  "kentico-xperience-integrations-lucene-admin": {
    "Mode": "Proxy",
    "Port": 3074
  }
}
```

The port must match `devServer.port` in `src/Kentico.Xperience.Lucene.Admin/Client/webpack.config.js`.

The Xperience web application requests client modules from a webpack dev server that runs parallel to the Xperience application.

Changes to client code are immediately integrated and don’t require a restart or rebuild of the web application.

Before you start developing, the webpack server needs to be manually started by running

```bash
npm ci
npm run start
```

from the root of the module folder, in our case in the `/src/Kentico.Xperience.Lucene.Admin/Client` folder.

> While proxy mode is configured, the Lucene admin UI pages will not load unless the webpack dev server is running.

### End-to-end tests

Playwright tests cover DancingGoat search and the Lucene administration. They require a licensed database and Chromium. See the [e2e guide](../tests/e2e/README.md) for setup, commands, and environment variables.

## Development Workflow

### Prepare your Git branch and commits

1. Create a new branch with one of the following prefixes

   - `feat/` - for new functionality
   - `refactor/` - for restructuring of existing features
   - `fix/` - for bugfixes

1. Run `dotnet format` against `Kentico.Xperience.Lucene.slnx`

   > use `.NET: format (Lucene)` VS Code task.

1. Commit changes, with a commit message preferably following the [Conventional Commits](https://www.conventionalcommits.org/en/v1.0.0/#summary) convention.

1. If you changed TypeScript, run `npm run format`, `npm run lint`, and `npm run typecheck` in `src/Kentico.Xperience.Lucene.Admin/Client` or `tests/e2e`.

The shared `.claude/skills` guidance is available to Copilot and Claude. Use `setup-local` for local database setup, `update-xperience` for Xperience version updates, and `update-template` to review template changes.

### Validate packages

The Release package build uses the same project targets as CI and Azure DevOps. It creates all three NuGet packages with the admin client and other embedded assets included:

```powershell
dotnet pack src/Kentico.Xperience.Lucene.Libs.slnx --configuration Release -p:SIGN_FILE=false
```

The normal Release build embeds the admin client and other required assets. For end-to-end testing, build and run DancingGoat with the solution workflow rather than substituting locally generated packages.

### Create a PR

Once ready, create a PR on GitHub. The PR will need to have all comments resolved and all tests passing before it will be merged.

- The PR should have a helpful description of the scope of changes being contributed.
- Include screenshots or video to reflect UX or UI updates
- Indicate if new settings need to be applied when the changes are merged - locally or in other environments
