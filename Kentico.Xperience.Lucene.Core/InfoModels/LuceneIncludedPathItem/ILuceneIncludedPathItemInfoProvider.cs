using CMS.DataEngine;

namespace Kentico.Xperience.Lucene.Core;

public partial interface ILuceneIncludedPathItemInfoProvider
{
    void BulkDelete(IWhereCondition where, BulkDeleteSettings? settings = null);
}
