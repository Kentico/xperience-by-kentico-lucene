# Xperience by Kentico: Dancing Goat Sample Project

This project implements a company website of a fictional coffee shop franchise to demonstrate the Xperience solution's content management and digital marketing features.

## Installation and setup

Follow the instructions in the [Installation](https://docs.xperience.io/x/DQKQC) documentation
to troubleshoot any installation or configuration issues.

## Project notes

### Content type and reusable field schema code files

[Content type](https://docs.xperience.io/x/gYHWCQ) and [reusable field schema](https://docs.xperience.io/x/D4_OD) code files under 

- `./Models/Reusable` 
- `./Models/WebPage`
- `./Models/Schema`

are generated using Xperience's [code generators](https://docs.xperience.io/x/5IbWCQ).

If you change the site's content model (add or remove fields, define new content types or schemas, etc.), you can run the following commands from the root of the Dancing Goat project to regenerate the files.

For _reusable field schemas_:

```powershell
dotnet run --no-build -- --kxp-codegen --location "./Models/Schema/" --type ReusableFieldSchemas --namespace "DancingGoat.Models"
```

This command regenerates the interfaces for all reusable field schemas in the project. Note that the specified `--namespace` must match the namespace where content type code files that reference the schemas are generated. You will get uncompilable code otherwise.

For _reusable_ content types:

```powershell
dotnet run --no-build -- --kxp-codegen --location "./Models/Reusable/{name}/" --type ReusableContentTypes --include "DancingGoat.*" --namespace "DancingGoat.Models"
```

This command generates code files for content types with the `DancingGoat` namespace under the `./Models/Reusable` directory.

For _page_ content types:

```powershell
dotnet run --no-build -- --kxp-codegen --location "./Models/WebPage/{name}/" --type PageContentTypes --include "DancingGoat.*" --namespace "DancingGoat.Models"
```

This command generates code files for content types with the `DancingGoat` namespace under the `./Models/WebPage` directory.

You can adapt these examples for use in projects with a different folder structure by modifying the `location` parameter accordingly.
