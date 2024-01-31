# Xperience by Kentico: Dancing Goat Sample Project

This project implements a company website of a fictional coffee shop franchise with the aim of demonstrating
the content management and digital marketing features of the Xperience solution.

## Installation and setup

Follow the instructions in the [Installation](https://docs.xperience.io/x/DQKQC) documentation
to troubleshoot any installation or configuration issues.

## Project notes

### Content type code files

[Content type](https://docs.xperience.io/x/gYHWCQ) code files under `./Models/Reusable` and `./Models/WebPage` are 
generated using [code generators](https://docs.xperience.io/x/5IbWCQ) provided by Xperience.

If you add new content types or make changes to existing ones (e.g., add or remove fields), you can
run the following commands from the root of the Dancing Goat project:

For _reusable_ content types:

```powershell
dotnet run --no-build -- --kxp-codegen --location "./Models/Reusable/{name}/" --type ReusableContentTypes --include "DancingGoat.*" --namespace "DancingGoat.Models"
```

This command generates code files for content types with the `DancingGoat` namespace under the `./Models/Reusable` directory.

You can use a similar approach for _page_ content types:

```powershell
dotnet run --no-build -- --kxp-codegen --location "./Models/WebPage/{name}/" --type PageContentTypes --include "DancingGoat.*" --namespace "DancingGoat.Models"
```

This command generates code files for content types with the `DancingGoat` namespace under the `./Models/WebPage` directory.

You can of course adapt these example for use in projects with a different folder structure by modifying the `location` parameter accordingly.
