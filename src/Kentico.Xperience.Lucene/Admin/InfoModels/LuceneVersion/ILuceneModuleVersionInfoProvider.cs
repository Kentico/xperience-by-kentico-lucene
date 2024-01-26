namespace Kentico.Xperience.Lucene.Admin;

public partial interface ILuceneModuleVersionInfoProvider
{
    LuceneModuleVersionInfo GetDatabaseModuleVersion();

    string GetAssemblyVersionNumber();
}
