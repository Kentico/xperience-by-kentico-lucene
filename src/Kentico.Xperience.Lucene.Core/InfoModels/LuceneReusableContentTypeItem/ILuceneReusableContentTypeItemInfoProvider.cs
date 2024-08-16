using CMS.DataEngine;

namespace Kentico.Xperience.Lucene.Core;

/// <summary>
/// Declares members for <see cref="LuceneReusableContentTypeItemInfo"/> management.
/// </summary>
public partial interface ILuceneReusableContentTypeItemInfoProvider
{
    void BulkDelete(IWhereCondition where, BulkDeleteSettings? settings = null);
}
