using CMS.DataEngine;

namespace Kentico.Xperience.Lucene.Core;

/// <summary>
/// Declares members for <see cref="LuceneContentTypeItemInfo"/> management.
/// </summary>
public partial interface ILuceneContentTypeItemInfoProvider
{
    /// <summary>
    /// Bulk deletes <see cref="LuceneContentTypeItemInfo"/> objects based on the given condition.
    /// </summary>
    /// <param name="where">Where condition for the objects which should be deleted.</param>
    /// <param name="settings">Configuration settings.</param>
    void BulkDelete(IWhereCondition where, BulkDeleteSettings? settings = null);
}
