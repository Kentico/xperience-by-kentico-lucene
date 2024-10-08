# Contributing Setup

## Required Software

The requirements to setup, develop, and build this project are listed below.

### .NET Runtime

.NET SDK 7.0 or newer

- <https://dotnet.microsoft.com/en-us/download/dotnet/7.0>
- See `global.json` file for specific SDK requirements

### Node.js Runtime

- [Node.js](https://nodejs.org/en/download) 18.12.0 or newer
- [NVM for Windows](https://github.com/coreybutler/nvm-windows) to manage multiple installed versions of Node.js
- See `engines` in the solution `package.json` for specific version requirements

### C# Editor

- VS Code
- Visual Studio
- Rider

### Database

SQL Server 2019 or newer compatible database

- [SQL Server Linux](https://learn.microsoft.com/en-us/sql/linux/sql-server-linux-setup?view=sql-server-ver15)
- [Azure SQL Edge](https://learn.microsoft.com/en-us/azure/azure-sql-edge/disconnected-deployment)

### SQL Editor

- MS SQL Server Management Studio
- Azure Data Studio

## Example Project

### Database Setup

Running the example project requires creating a new Xperience by Kentico database using the included template.

Change directory in your console to `./examples/DancingGoat` and follow the instructions in the Xperience
documentation on [creating a new database](https://docs.xperience.io/xp26/developers-and-admins/installation#Installation-CreatetheprojectdatabaseCreateProjectDatabase).

### Admin Customization

To run the example project Admin customization in development mode, add the following to your [User Secrets](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets?view=aspnetcore-7.0&tabs=windows#secret-manager) for the application.

```json
"CMSAdminClientModuleSettings": {
  "kentico-xperience-integrations-lucene-admin": {
    "Mode": "Proxy",
    "Port": 3009
  }
}
```

The Xperience web application requests client modules from a webpack dev server that runs parallel to the Xperience application.

Changes to client code are immediately integrated and don’t require a restart or rebuild of the web application. 

Before you start developing, the webpack server needs to be manually started by running

```bash
npm run start
```
from the root of the module folder, in our case in the `/src/Kentico.Xperience.Lucene.Admin/Client` folder.

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
   This will generate a new `Kentico.Xperience.Lucene.Admin` package in the `nuget-local` folder with a version matching the version in your `Directory.Build.props`

1. Update the `Directory.Packages.props` to populate the `Kentico.Xperience.Lucene.Admin` package `Version=""` with the matching the value from the project's `Directory.Build.props`

   > In the future, we will be able to use floating versions to automatically select the highest (local) package version

1. Build the solution with the `LOCAL_NUGET=true` property

   > You can use the VS Code `.NET: build (Solution) - LOCAL_NUGET` task

1. Make sure the `Kentico.Xperience.Lucene.Admin.dll` version in the `examples\DancingGoat\bin\Debug\net6.0\` folder is the right version

1. Run the `DancingGoat` application and ensure all functionality is correct

   > You can use the `.NET Launch (DancingGoat) - LOCAL_NUGET` lauch setting in VS Code

1. Undo the `Directory.Packages.props` version number change to ensure it is not committed to the repository

1. Perform a normal build to reset any modified `packages.lock.json` files

### Create a PR

Once ready, create a PR on GitHub. The PR will need to have all comments resolved and all tests passing before it will be merged.

- The PR should have a helpful description of the scope of changes being contributed.
- Include screenshots or video to reflect UX or UI updates
- Indicate if new settings need to be applied when the changes are merged - locally or in other environments
