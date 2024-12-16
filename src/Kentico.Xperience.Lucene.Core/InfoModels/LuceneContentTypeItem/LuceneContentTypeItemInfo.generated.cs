using System.Data;
using System.Runtime.Serialization;

using CMS;
using CMS.DataEngine;
using CMS.Helpers;

using Kentico.Xperience.Lucene.Core;


[assembly: RegisterObjectType(typeof(LuceneContentTypeItemInfo), LuceneContentTypeItemInfo.OBJECT_TYPE)]

namespace Kentico.Xperience.Lucene.Core;

/// <summary>
/// Data container class for <see cref="LuceneContentTypeItemInfo"/>.
/// </summary>
[Serializable]
public partial class LuceneContentTypeItemInfo : AbstractInfo<LuceneContentTypeItemInfo, IInfoProvider<LuceneContentTypeItemInfo>>
{
    /// <summary>
    /// Object type.
    /// </summary>
    public const string OBJECT_TYPE = "kenticolucene.lucenecontenttypeitem";


    /// <summary>
    /// Type information.
    /// </summary>
    public static readonly ObjectTypeInfo TYPEINFO = new(typeof(IInfoProvider<LuceneContentTypeItemInfo>), OBJECT_TYPE, "KenticoLucene.LuceneContentTypeItem", nameof(LuceneContentTypeItemId), null, nameof(LuceneContentTypeItemGuid), null, null, null, null, null)
    {
        TouchCacheDependencies = true,
        DependsOn = new List<ObjectDependency>()
        {
            new(nameof(LuceneContentTypeItemIncludedPathItemId), LuceneIncludedPathItemInfo.OBJECT_TYPE, ObjectDependencyEnum.Required),
            new(nameof(LuceneContentTypeItemIndexItemId), LuceneIndexItemInfo.OBJECT_TYPE, ObjectDependencyEnum.Required),
        },
        ContinuousIntegrationSettings =
        {
            Enabled = true
        }
    };


    /// <summary>
    /// Lucene content type item id.
    /// </summary>
    [DatabaseField]
    public virtual int LuceneContentTypeItemId
    {
        get => ValidationHelper.GetInteger(GetValue(nameof(LuceneContentTypeItemId)), 0);
        set => SetValue(nameof(LuceneContentTypeItemId), value);
    }


    /// <summary>
    /// Lucene content type item guid.
    /// </summary>
    [DatabaseField]
    public virtual Guid LuceneContentTypeItemGuid
    {
        get => ValidationHelper.GetGuid(GetValue(nameof(LuceneContentTypeItemGuid)), default);
        set => SetValue(nameof(LuceneContentTypeItemGuid), value);
    }


    /// <summary>
    /// Content type name.
    /// </summary>
    [DatabaseField]
    public virtual string LuceneContentTypeItemContentTypeName
    {
        get => ValidationHelper.GetString(GetValue(nameof(LuceneContentTypeItemContentTypeName)), String.Empty);
        set => SetValue(nameof(LuceneContentTypeItemContentTypeName), value);
    }


    /// <summary>
    /// Lucene included path item id.
    /// </summary>
    [DatabaseField]
    public virtual int LuceneContentTypeItemIncludedPathItemId
    {
        get => ValidationHelper.GetInteger(GetValue(nameof(LuceneContentTypeItemIncludedPathItemId)), 0);
        set => SetValue(nameof(LuceneContentTypeItemIncludedPathItemId), value);
    }


    /// <summary>
    /// Lucene index item id.
    /// </summary>
    [DatabaseField]
    public virtual int LuceneContentTypeItemIndexItemId
    {
        get => ValidationHelper.GetInteger(GetValue(nameof(LuceneContentTypeItemIndexItemId)), 0);
        set => SetValue(nameof(LuceneContentTypeItemIndexItemId), value);
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
    /// Creates an empty instance of the <see cref="LuceneContentTypeItemInfo"/> class.
    /// </summary>
    public LuceneContentTypeItemInfo()
        : base(TYPEINFO)
    {
    }


    /// <summary>
    /// Creates a new instances of the <see cref="LuceneContentTypeItemInfo"/> class from the given <see cref="DataRow"/>.
    /// </summary>
    /// <param name="dr">DataRow with the object data.</param>
    public LuceneContentTypeItemInfo(DataRow dr)
        : base(TYPEINFO, dr)
    {
    }
}
