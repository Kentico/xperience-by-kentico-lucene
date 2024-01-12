using CMS.DataEngine;

namespace Kentico.Xperience.Lucene.Admin;

public partial interface ILuceneIndexItemInfoProvider
{
    void BulkDelete(IWhereCondition where, BulkDeleteSettings? settings = null);
}
