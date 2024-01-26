using CMS.Modules;

namespace Kentico.Xperience.Lucene.Admin;

internal class LuceneModuleMigrator
{
    private readonly ILuceneModuleVersionInfoProvider versionProvider;
    private readonly IResourceInfoProvider resourceProvider;
    private readonly LuceneModuleInstaller moduleInstaller;

    public LuceneModuleMigrator(
        ILuceneModuleVersionInfoProvider versionProvider,
        IResourceInfoProvider resourceProvider,
        LuceneModuleInstaller moduleInstaller)
    {
        this.versionProvider = versionProvider;
        this.resourceProvider = resourceProvider;
        this.moduleInstaller = moduleInstaller;
    }

    public void ValidateModuleInstall()
    {
        var status = versionProvider.GetModuleInstallationStatus();

        if (status.IsInstallationValid())
        {
            return;
        }

        string errorMessage = $"""
            The {versionProvider.GetAssemblyName()} integration assembly does not match the installed version.
            Installed version - {status.DatabaseVersion}
            Package version - {status.AssemblyVersion}

            You must first run "dotnet run --kxp-update" to update this integration from the package.
            """;

        throw new InvalidOperationException(errorMessage);
    }

    public void Migrate()
    {
        var status = versionProvider.GetModuleInstallationStatus();

        while (!string.Equals(status.DatabaseVersion, status.AssemblyVersion))
        {
            try
            {
                _ = status switch
                {
                    (false, _) => Install(Migrate_None_to_Latest, status),
                    (true, "4.0.0") => Migrate(Migrate_4_0_0_to_4_1_0, status),
                    _ => throw new InvalidOperationException($"{versionProvider.GetAssemblyName()} v{status.DatabaseVersion} cannot be migrated")
                };
            }
            catch
            {
                string message = $"Upgrading {versionProvider.GetAssemblyName()} from v{status.DatabaseVersion} to v{status.AssemblyVersion} failed";

                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(message);
                Console.ResetColor();

                throw;
            }

            status = versionProvider.GetModuleInstallationStatus();
        }

        Console.ForegroundColor = ConsoleColor.DarkCyan;
        Console.WriteLine($"{versionProvider.GetAssemblyName()} is up to date");
        Console.ResetColor();
    }

    private string Install(Func<string> installation, InstallStatus status)
    {
        Console.ForegroundColor = ConsoleColor.DarkCyan;
        Console.WriteLine($"Installing {versionProvider.GetAssemblyName()} v{status.AssemblyVersion}");

        string newVersion = installation();

        Console.WriteLine($"Install complete");
        Console.ResetColor();

        return newVersion;
    }

    private string Migrate(Func<string> migration, InstallStatus status)
    {
        Console.ForegroundColor = ConsoleColor.DarkCyan;
        Console.WriteLine($"Upgrading {versionProvider.GetAssemblyName()} from v{status.DatabaseVersion} to v{status.AssemblyVersion}");

        string newVersion = migration();

        Console.WriteLine($"Upgraded complete");
        Console.ResetColor();

        return newVersion;
    }

    private string Migrate_None_to_Latest()
    {
        moduleInstaller.Install();

        string assemblyVersionNumber = versionProvider.GetAssemblyVersionNumber();

        var initialVersion = new LuceneModuleVersionInfo
        {
            LuceneModuleVersionNumber = assemblyVersionNumber
        };
        versionProvider.Set(initialVersion);

        return initialVersion.LuceneModuleVersionNumber;
    }

    private string Migrate_4_0_0_to_4_1_0()
    {
        var resource = resourceProvider.Get("Kentico.Xperience.Lucene");
        // Fix the module name
        moduleInstaller.InitializeResource(resource);

        // Update all the data types to use ClassType.SYSTEM_TABLE
        moduleInstaller.InstallLuceneModuleVersionInfo(resource);
        moduleInstaller.InstallLuceneItemInfo(resource);
        moduleInstaller.InstallLuceneLanguageInfo(resource);
        moduleInstaller.InstallLuceneIndexPathItemInfo(resource);
        moduleInstaller.InstallLuceneContentTypeItemInfo(resource);
        moduleInstaller.InstallLuceneModuleVersionInfo(resource);

        string newVersionNumber = "4.1.0";

        var newVersionInfo = new LuceneModuleVersionInfo
        {
            LuceneModuleVersionNumber = newVersionNumber,
        };
        versionProvider.Set(newVersionInfo);

        return newVersionInfo.LuceneModuleVersionNumber;
    }
}
