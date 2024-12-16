using System.Data;
using System.Runtime.Serialization;

using CMS;
using CMS.DataEngine;
using CMS.Helpers;

using Kentico.Xperience.Lucene.Core;


[assembly: RegisterObjectType(typeof(LuceneReusableContentTypeItemInfo), LuceneReusableContentTypeItemInfo.OBJECT_TYPE)]

namespace Kentico.Xperience.Lucene.Core;

/// <summary>
/// Data container class for <see cref="LuceneReusableContentTypeItemInfo"/>.
/// </summary>
[Serializable]
public partial class LuceneReusableContentTypeItemInfo : AbstractInfo<LuceneReusableContentTypeItemInfo, IInfoProvider<LuceneReusableContentTypeItemInfo>>
{
    /// <summary>
    /// Object type.
    /// </summary>
    public const string OBJECT_TYPE = "kenticolucene.lucenereusablecontenttypeitem";


    /// <summary>
    /// Type information.
    /// </summary>
    public static readonly ObjectTypeInfo TYPEINFO = new(typeof(IInfoProvider<LuceneReusableContentTypeItemInfo>), OBJECT_TYPE, "KenticoLucene.LuceneReusableContentTypeItem", nameof(LuceneReusableContentTypeItemId), null, nameof(LuceneReusableContentTypeItemGuid), null, null, null, null, null)
    {
        TouchCacheDependencies = true,
        DependsOn = new List<ObjectDependency>()
        {
            new(nameof(LuceneReusableContentTypeItemIndexItemId), LuceneIndexItemInfo.OBJECT_TYPE, ObjectDependencyEnum.Required),
        },
        ContinuousIntegrationSettings =
        {
            Enabled = true
        }
    };


    /// <summary>
    /// Lucene reusable content type item id.
    /// </summary>
    [DatabaseField]
    public virtual int LuceneReusableContentTypeItemId
    {
        get => ValidationHelper.GetInteger(GetValue(nameof(LuceneReusableContentTypeItemId)), 0);
        set => SetValue(nameof(LuceneReusableContentTypeItemId), value);
    }


    /// <summary>
    /// Lucene reusable content type item guid.
    /// </summary>
    [DatabaseField]
    public virtual Guid LuceneReusableContentTypeItemGuid
    {
        get => ValidationHelper.GetGuid(GetValue(nameof(LuceneReusableContentTypeItemGuid)), default);
        set => SetValue(nameof(LuceneReusableContentTypeItemGuid), value);
    }


    /// <summary>
    /// Reusable content type name.
    /// </summary>
    [DatabaseField]
    public virtual string LuceneReusableContentTypeItemContentTypeName
    {
        get => ValidationHelper.GetString(GetValue(nameof(LuceneReusableContentTypeItemContentTypeName)), String.Empty);
        set => SetValue(nameof(LuceneReusableContentTypeItemContentTypeName), value);
    }


    /// <summary>
    /// Lucene index item id.
    /// </summary>
    [DatabaseField]
    public virtual int LuceneReusableContentTypeItemIndexItemId
    {
        get => ValidationHelper.GetInteger(GetValue(nameof(LuceneReusableContentTypeItemIndexItemId)), 0);
        set => SetValue(nameof(LuceneReusableContentTypeItemIndexItemId), value);
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
    /// Creates an empty instance of the <see cref="LuceneReusableContentTypeItemInfo"/> class.
    /// </summary>
    public LuceneReusableContentTypeItemInfo()
        : base(TYPEINFO)
    {
    }


    /// <summary>
    /// Creates a new instances of the <see cref="LuceneReusableContentTypeItemInfo"/> class from the given <see cref="DataRow"/>.
    /// </summary>
    /// <param name="dr">DataRow with the object data.</param>
    public LuceneReusableContentTypeItemInfo(DataRow dr)
        : base(TYPEINFO, dr)
    {
    }
}
