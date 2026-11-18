# Contributing Setup

## Required Software

The requirements to setup, develop, and build this project are listed below.

### .NET SDK

.NET SDK 10.0 or newer

- <https://dotnet.microsoft.com/en-us/download/dotnet/10.0>
- See the `global.json` file for specific SDK requirements

### Node.js Runtime

- [Node.js](https://nodejs.org/en/download) 24 (`>=24.22.1 <25`)
- [NVM for Windows](https://github.com/coreybutler/nvm-windows) to manage multiple installed versions of Node.js
- See `engines` in `src/Kentico.Xperience.Lucene.Admin/Client/package.json` for specific version requirements

### C# Editor

- VS Code
- Visual Studio
- Rider

### Database

SQL Server 2019 or newer compatible database

- [SQL Server Linux](https://learn.microsoft.com/en-us/sql/linux/sql-server-linux-setup)
- [SQL Server in Docker](https://learn.microsoft.com/en-us/sql/linux/quickstart-install-connect-docker)

### SQL Editor

- MS SQL Server Management Studio
- VS Code with the [SQL Server (mssql)](https://marketplace.visualstudio.com/items?itemName=ms-mssql.mssql) extension

## Example Project

See the [DancingGoat README](../examples/DancingGoat/README.md) for full setup steps.

### Database Setup

Running the example project requires creating a new Xperience by Kentico database using the included template.

Change directory in your console to `./examples/DancingGoat`, restore the database tools, and create the database:

```powershell
dotnet tool restore
dotnet kentico-xperience-dbmanager -- -s "<sql_server_name>" -d "<database_name>" -a "Pass@12345" --hash-string-salt "<hash_string_salt>" --license-file "<license_file_path>"
```

See the Xperience documentation on [creating the project database](https://docs.kentico.com/documentation/developers-and-admins/installation#create-the-project-database) for all parameters.

Then enable Continuous Integration and restore the example's search objects (index, search page, article topics) from `App_Data/CIRepository`:

```powershell
dotnet run -- --kxp-ci-enable
dotnet run -- --kxp-ci-restore
```

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
npm install
npm run start
```

from the root of the module folder, in our case in the `/src/Kentico.Xperience.Lucene.Admin/Client` folder.

> While proxy mode is configured, the Lucene admin UI pages will not load unless the webpack dev server is running.

## Development Workflow

### Prepare your Git branch and commits

1. Create a new branch with one of the following prefixes

   - `feat/` - for new functionality
   - `refactor/` - for restructuring of existing features
   - `fix/` - for bugfixes

1. Run `dotnet format` against the `Kentico.Xperience.Lucene` solution

   > use `.NET: format (Lucene)` VS Code task.

1. Commit changes, with a commit message preferably following the [Conventional Commits](https://www.conventionalcommits.org/en/v1.0.0/#summary) convention.

### Test the package locally using the following commands

1. Generate a local package using the VS Code `.NET: pack (Lucene)` task or execute its command and arguments at the command line
   This will generate a new `Kentico.Xperience.Lucene` package in the `nuget-local` folder with a version matching the version in your `Directory.Build.props`

1. Update the `Directory.Packages.props` to populate the `Kentico.Xperience.Lucene` package `Version=""` with the matching the value from the project's `Directory.Build.props`

   > In the future, we will be able to use floating versions to automatically select the highest (local) package version

1. Build the solution with the `LOCAL_NUGET=true` property

   > You can use the VS Code `.NET: build (Solution) - LOCAL_NUGET` task

1. Make sure the `Kentico.Xperience.Lucene.dll` version in the `examples\DancingGoat\bin\Debug\net10.0\` folder is the right version

1. Run the `DancingGoat` application and ensure all functionality is correct

   > You can use the `.NET Launch (DancingGoat) - LOCAL_NUGET` launch setting in VS Code

1. Undo the `Directory.Packages.props` version number change to ensure it is not committed to the repository

1. Perform a normal build to reset any modified `packages.lock.json` files

### Create a PR

Once ready, create a PR on GitHub. The PR will need to have all comments resolved and all tests passing before it will be merged.

- The PR should have a helpful description of the scope of changes being contributed.
- Include screenshots or video to reflect UX or UI updates
- Indicate if new settings need to be applied when the changes are merged - locally or in other environments
