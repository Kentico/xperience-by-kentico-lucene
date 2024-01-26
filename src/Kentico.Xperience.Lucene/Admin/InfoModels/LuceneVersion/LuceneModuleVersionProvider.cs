using System.Diagnostics;
using System.Reflection;

namespace Kentico.Xperience.Lucene.Admin;

public partial class LuceneModuleVersionInfoProvider : ILuceneModuleVersionInfoProvider
{
    public LuceneModuleVersionInfo GetDatabaseModuleVersion()
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
}
