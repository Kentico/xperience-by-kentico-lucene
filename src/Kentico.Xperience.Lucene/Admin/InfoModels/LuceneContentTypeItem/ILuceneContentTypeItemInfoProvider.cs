using CMS.DataEngine;

namespace Kentico.Xperience.Lucene.Admin;

/// <summary>
/// Declares members for <see cref="LuceneContentTypeItemInfo"/> management.
/// </summary>
public partial interface ILuceneContentTypeItemInfoProvider
{
    void BulkDelete(IWhereCondition where, BulkDeleteSettings? settings = null);
}
