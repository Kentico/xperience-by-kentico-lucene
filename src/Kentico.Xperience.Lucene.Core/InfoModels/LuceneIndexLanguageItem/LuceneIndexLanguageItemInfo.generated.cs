using System.Data;
using System.Runtime.Serialization;

using CMS;
using CMS.DataEngine;
using CMS.Helpers;

using Kentico.Xperience.Lucene.Core;

[assembly: RegisterObjectType(typeof(LuceneIndexLanguageItemInfo), LuceneIndexLanguageItemInfo.OBJECT_TYPE)]

namespace Kentico.Xperience.Lucene.Core;

/// <summary>
/// Data container class for <see cref="LuceneIndexLanguageItemInfo"/>.
/// </summary>
[Serializable]
public partial class LuceneIndexLanguageItemInfo : AbstractInfo<LuceneIndexLanguageItemInfo, IInfoProvider<LuceneIndexLanguageItemInfo>>
{
    /// <summary>
    /// Object type.
    /// </summary>
    public const string OBJECT_TYPE = "kenticolucene.luceneindexlanguageitem";


    /// <summary>
    /// Type information.
    /// </summary>
    public static readonly ObjectTypeInfo TYPEINFO = new(typeof(IInfoProvider<LuceneIndexLanguageItemInfo>), OBJECT_TYPE, "KenticoLucene.LuceneIndexLanguageItem", nameof(LuceneIndexLanguageItemID), null, nameof(LuceneIndexLanguageItemGuid), null, null, null, null, null)
    {
        TouchCacheDependencies = true,
        DependsOn = new List<ObjectDependency>()
        {
            new(nameof(LuceneIndexLanguageItemIndexItemId), LuceneIndexItemInfo.OBJECT_TYPE, ObjectDependencyEnum.Required),
        },
        ContinuousIntegrationSettings =
        {
            Enabled = true
        }
    };


    /// <summary>
    /// Indexed language id.
    /// </summary>
    [DatabaseField]
    public virtual int LuceneIndexLanguageItemID
    {
        get => ValidationHelper.GetInteger(GetValue(nameof(LuceneIndexLanguageItemID)), 0);
        set => SetValue(nameof(LuceneIndexLanguageItemID), value);
    }


    /// <summary>
    /// Indexed language id.
    /// </summary>
    [DatabaseField]
    public virtual Guid LuceneIndexLanguageItemGuid
    {
        get => ValidationHelper.GetGuid(GetValue(nameof(LuceneIndexLanguageItemGuid)), default);
        set => SetValue(nameof(LuceneIndexLanguageItemGuid), value);
    }


    /// <summary>
    /// Code.
    /// </summary>
    [DatabaseField]
    public virtual string LuceneIndexLanguageItemName
    {
        get => ValidationHelper.GetString(GetValue(nameof(LuceneIndexLanguageItemName)), String.Empty);
        set => SetValue(nameof(LuceneIndexLanguageItemName), value);
    }


    /// <summary>
    /// Lucene index item id.
    /// </summary>
    [DatabaseField]
    public virtual int LuceneIndexLanguageItemIndexItemId
    {
        get => ValidationHelper.GetInteger(GetValue(nameof(LuceneIndexLanguageItemIndexItemId)), 0);
        set => SetValue(nameof(LuceneIndexLanguageItemIndexItemId), value);
    }


    /// <summary>
    /// Deletes the object using appropriate provider.
    /// </summary>
    protected override void DeleteObject()
    {
        Provider.Delete(this);
    }


    /// <summary>
    /// Updates the object using appropriate provider.
    /// </summary>
    protected override void SetObject()
    {
        Provider.Set(this);
    }


    /// <summary>
    /// Creates an empty instance of the <see cref="LuceneIndexLanguageItemInfo"/> class.
    /// </summary>
    public LuceneIndexLanguageItemInfo()
        : base(TYPEINFO)
    {
    }


    /// <summary>
    /// Creates a new instances of the <see cref="LuceneIndexLanguageItemInfo"/> class from the given <see cref="DataRow"/>.
    /// </summary>
    /// <param name="dr">DataRow with the object data.</param>
    public LuceneIndexLanguageItemInfo(DataRow dr)
        : base(TYPEINFO, dr)
    {
    }
}
