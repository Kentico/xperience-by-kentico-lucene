using CMS.DataEngine;

namespace Kentico.Xperience.Lucene.Admin;

public partial interface ILuceneIncludedPathItemInfoProvider
{
    void BulkDelete(IWhereCondition where, BulkDeleteSettings? settings = null);
}
