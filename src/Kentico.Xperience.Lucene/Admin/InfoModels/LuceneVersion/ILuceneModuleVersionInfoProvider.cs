namespace Kentico.Xperience.Lucene.Admin;

public partial interface ILuceneModuleVersionInfoProvider
{
    InstallStatus GetModuleInstallationStatus();

    LuceneModuleVersionInfo GetInstalledModuleVersion();

    string GetAssemblyVersionNumber();

    string GetAssemblyName();
}
