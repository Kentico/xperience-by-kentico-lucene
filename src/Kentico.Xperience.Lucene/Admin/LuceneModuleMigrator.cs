using CMS.Base;
using CMS.Modules;

namespace Kentico.Xperience.Lucene.Admin;

internal class LuceneModuleMigrator
{
    private readonly ILuceneModuleVersionInfoProvider versionProvider;
    private readonly IResourceInfoProvider resourceProvider;

    public LuceneModuleMigrator(ILuceneModuleVersionInfoProvider versionProvider, IResourceInfoProvider resourceProvider)
    {
        this.versionProvider = versionProvider;
        this.resourceProvider = resourceProvider;
    }

    public void Migrate(MigrationExecutionEventArgs _)
    {
        string assemblyVersion = versionProvider.GetAssemblyVersionNumber();
        string installedVersion = versionProvider.GetDatabaseModuleVersion().LuceneModuleVersionNumber;

        while (!string.Equals(installedVersion, assemblyVersion))
        {
            try
            {
                installedVersion = installedVersion switch
                {
                    "3.0.0" => Migrate_3_0_0_to_4_0_0(),
                    "4.0.0" => Migrate_4_0_0_to_4_0_1(),
                    _ => assemblyVersion
                };

                Console.ForegroundColor = ConsoleColor.DarkCyan;
                Console.WriteLine($"Upgrading Kentico.Xperience.Lucene from {installedVersion} to version {assemblyVersion}");
                Console.ResetColor();
            }
            catch
            {
                string message = $"Upgrading Kentico.Xperience.Lucene from {installedVersion} to version {assemblyVersion} failed";

                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(message);
                Console.ResetColor();

                throw;
            }
        }

    }

    private string Migrate_4_0_0_to_4_0_1()
    {
        var oldResource = resourceProvider.Get("Kentico.Xperience.Lucene");

        oldResource.ResourceName = "CMS.Integration.Lucene";
        resourceProvider.Set(oldResource);

        string newVersion = "4.0.1";

        UpdateVersion(newVersion);

        return newVersion;
    }

    private string Migrate_3_0_0_to_4_0_0()
    {
        string newVersion = "4.0.0";

        UpdateVersion(newVersion);

        return newVersion;
    }

    private void UpdateVersion(string newVersion)
    {
        var moduleVersion = versionProvider.GetDatabaseModuleVersion();

        moduleVersion.LuceneModuleVersionNumber = newVersion;
        versionProvider.Set(moduleVersion);
    }
}
