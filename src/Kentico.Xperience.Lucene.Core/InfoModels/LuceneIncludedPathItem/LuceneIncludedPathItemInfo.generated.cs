using System.Data;
using System.Runtime.Serialization;

using CMS;
using CMS.DataEngine;
using CMS.Helpers;

using Kentico.Xperience.Lucene.Core;

[assembly: RegisterObjectType(typeof(LuceneIncludedPathItemInfo), LuceneIncludedPathItemInfo.OBJECT_TYPE)]

namespace Kentico.Xperience.Lucene.Core;

/// <summary>
/// Data container class for <see cref="LuceneIncludedPathItemInfo"/>.
/// </summary>
[Serializable]
public partial class LuceneIncludedPathItemInfo : AbstractInfo<LuceneIncludedPathItemInfo, IInfoProvider<LuceneIncludedPathItemInfo>>
{
    /// <summary>
    /// Object type.
    /// </summary>
    public const string OBJECT_TYPE = "kenticolucene.luceneincludedpathitem";


    /// <summary>
    /// Type information.
    /// </summary>
    public static readonly ObjectTypeInfo TYPEINFO = new(typeof(IInfoProvider<LuceneIncludedPathItemInfo>), OBJECT_TYPE, "KenticoLucene.LuceneIncludedPathItem", nameof(LuceneIncludedPathItemId), null, nameof(LuceneIncludedPathItemGuid), null, null, null, null, null)
    {
        TouchCacheDependencies = true,
        DependsOn = new List<ObjectDependency>()
        {
            new(nameof(LuceneIncludedPathItemIndexItemId), LuceneIndexItemInfo.OBJECT_TYPE, ObjectDependencyEnum.Required),
        },
        ContinuousIntegrationSettings =
        {
            Enabled = true
        }
    };


    /// <summary>
    /// Lucene included path item id.
    /// </summary>
    [DatabaseField]
    public virtual int LuceneIncludedPathItemId
    {
        get => ValidationHelper.GetInteger(GetValue(nameof(LuceneIncludedPathItemId)), 0);
        set => SetValue(nameof(LuceneIncludedPathItemId), value);
    }

    /// <summary>
    /// Lucene included path item guid.
    /// </summary>
    [DatabaseField]
    public virtual Guid LuceneIncludedPathItemGuid
    {
        get => ValidationHelper.GetGuid(GetValue(nameof(LuceneIncludedPathItemGuid)), default);
        set => SetValue(nameof(LuceneIncludedPathItemGuid), value);
    }


    /// <summary>
    /// Alias path.
    /// </summary>
    [DatabaseField]
    public virtual string LuceneIncludedPathItemAliasPath
    {
        get => ValidationHelper.GetString(GetValue(nameof(LuceneIncludedPathItemAliasPath)), String.Empty);
        set => SetValue(nameof(LuceneIncludedPathItemAliasPath), value);
    }


    /// <summary>
    /// Lucene index item id.
    /// </summary>
    [DatabaseField]
    public virtual int LuceneIncludedPathItemIndexItemId
    {
        get => ValidationHelper.GetInteger(GetValue(nameof(LuceneIncludedPathItemIndexItemId)), 0);
        set => SetValue(nameof(LuceneIncludedPathItemIndexItemId), value);
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
    /// Creates an empty instance of the <see cref="LuceneIncludedPathItemInfo"/> class.
    /// </summary>
    public LuceneIncludedPathItemInfo()
        : base(TYPEINFO)
    {
    }


    /// <summary>
    /// Creates a new instances of the <see cref="LuceneIncludedPathItemInfo"/> class from the given <see cref="DataRow"/>.
    /// </summary>
    /// <param name="dr">DataRow with the object data.</param>
    public LuceneIncludedPathItemInfo(DataRow dr)
        : base(TYPEINFO, dr)
    {
    }
}
