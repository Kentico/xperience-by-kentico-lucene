using System.Diagnostics;
using System.Reflection;
using CMS.Base;

namespace Kentico.Xperience.Lucene.Admin;

internal class LuceneModuleMigrator
{
    private readonly ILuceneModuleVersionInfoProvider versionProvider;

    public LuceneModuleMigrator(ILuceneModuleVersionInfoProvider versionProvider) => this.versionProvider = versionProvider;

    public void Migrate(MigrationExecutionEventArgs _)
    {
        string assemblyVersion = GetAssemblyVersionNumber();
        string installedVersion = versionProvider.GetInstalledModuleVersion().LuceneModuleVersionNumber;

        while (!string.Equals(installedVersion, assemblyVersion))
        {
            try
            {
                installedVersion = installedVersion switch
                {
                    "3.0.0" => Migrate_3_0_0_to_4_0_0(),
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

    private string Migrate_3_0_0_to_4_0_0()
    {
        // Ex: Do migrations

        string newVersion = "4.0.0";

        var moduleVersion = versionProvider.GetInstalledModuleVersion();

        moduleVersion.LuceneModuleVersionNumber = newVersion;
        versionProvider.Set(moduleVersion);

        return newVersion;
    }

    public static string GetAssemblyVersionNumber()
    {
        var fileVersion = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location);
        return new Version(fileVersion.FileVersion ?? "").ToString(3);
    }
}
