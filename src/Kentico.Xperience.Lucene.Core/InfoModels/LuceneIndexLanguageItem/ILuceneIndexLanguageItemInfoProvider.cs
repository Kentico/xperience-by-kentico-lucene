using CMS.DataEngine;

namespace Kentico.Xperience.Lucene.Core;

/// <summary>
/// Declares members for <see cref="LuceneIndexLanguageItemInfo"/> management.
/// </summary>
public partial interface ILuceneIndexLanguageItemInfoProvider
{
    /// <summary>
    /// Bulk deletes <see cref="LuceneIndexLanguageItemInfo"/> objects based on the given condition.
    /// </summary>
    /// <param name="where">Where condition for the objects which should be deleted.</param>
    /// <param name="settings">Configuration settings.</param>
    void BulkDelete(IWhereCondition where, BulkDeleteSettings? settings = null);
}
