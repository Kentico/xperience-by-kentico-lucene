namespace Kentico.Xperience.Lucene.Admin;

public partial class LuceneModuleVersionInfoProvider : ILuceneModuleVersionInfoProvider
{
    public LuceneModuleVersionInfo GetInstalledModuleVersion()
    {
        var infos = Get().GetEnumerableTypedResult().ToList();

        if (infos.Count != 1)
        {
            throw new ArgumentException($"Expected 1 module version record but found {infos.Count}");
        }

        return infos[0];
    }
}
