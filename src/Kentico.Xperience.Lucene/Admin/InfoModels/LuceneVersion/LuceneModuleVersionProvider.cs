using System.Diagnostics;
using System.Reflection;
using CMS.Core;
using CMS.DataEngine;

namespace Kentico.Xperience.Lucene.Admin;

public partial class LuceneModuleVersionInfoProvider : ILuceneModuleVersionInfoProvider
{
    private readonly IConversionService conversion;

    public LuceneModuleVersionInfoProvider(IConversionService conversion) => this.conversion = conversion;

    public InstallStatus GetModuleInstallationStatus()
    {
        string queryText = $"""
            IF OBJECT_ID('KenticoLucene_LuceneModuleVersion', 'U') IS NOT NULL 
                SELECT 1 as IsInstalled, LuceneModuleVersionNumber FROM KenticoLucene_LuceneModuleVersion;
            ELSE IF OBJECT_ID('KenticoLucene_LuceneIndexItem', 'U') IS NOT NULL
                SELECT 1 as IsInstalled, '4.0.0' as LuceneModuleVersionNumber;
            ELSE
                SELECT 0 as IsInstalled, '0.0.0' as LuceneModuleVersionNumber;
            """;
        var ds = ConnectionHelper.ExecuteQuery(queryText, [], QueryTypeEnum.SQLQuery);

        bool isInstalled = conversion.GetBoolean(ds.Tables[0].Rows[0][0], false);
        string databaseVersion = conversion.GetString(ds.Tables[0].Rows[0][1], "");
        string assemblyVersion = GetAssemblyVersionNumber();

        return new(isInstalled, databaseVersion, assemblyVersion);
    }

    public LuceneModuleVersionInfo GetInstalledModuleVersion()
    {
        var infos = Get().GetEnumerableTypedResult().ToList();

        if (infos.Count != 1)
        {
            throw new InvalidOperationException($"Expected 1 module version record but found {infos.Count}");
        }

        return infos[0];
    }

    public string GetAssemblyVersionNumber()
    {
        var fileVersion = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location);
        return new Version(fileVersion.FileVersion ?? "").ToString(3);
    }

    public string GetAssemblyName() => Assembly.GetExecutingAssembly().GetName().Name ?? "";
}

public record InstallStatus(bool IsInstalled, string DatabaseVersion, string AssemblyVersion)
{
    public void Deconstruct(out bool isInstalled, out string databaseVersion)
    {
        isInstalled = IsInstalled;
        databaseVersion = DatabaseVersion;
    }

    public bool IsInstallationValid() => IsInstalled && string.Equals(DatabaseVersion, AssemblyVersion);
}
