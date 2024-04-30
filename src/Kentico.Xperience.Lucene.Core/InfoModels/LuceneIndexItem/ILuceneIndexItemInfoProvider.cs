using CMS.DataEngine;

namespace Kentico.Xperience.Lucene.Core;

public partial interface ILuceneIndexItemInfoProvider
{
    void BulkDelete(IWhereCondition where, BulkDeleteSettings? settings = null);
}
