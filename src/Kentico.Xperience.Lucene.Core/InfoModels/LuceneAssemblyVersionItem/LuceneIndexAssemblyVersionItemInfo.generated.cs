using System.Data;
using System.Runtime.Serialization;

using CMS;
using CMS.DataEngine;
using CMS.Helpers;

using Kentico.Xperience.Lucene.Core;


[assembly: RegisterObjectType(typeof(LuceneIndexAssemblyVersionItemInfo), LuceneIndexAssemblyVersionItemInfo.OBJECT_TYPE)]

namespace Kentico.Xperience.Lucene.Core;

/// <summary>
/// Data container class for <see cref="LuceneIndexAssemblyVersionItemInfo"/>.
/// </summary>
public partial class LuceneIndexAssemblyVersionItemInfo : AbstractInfo<LuceneIndexAssemblyVersionItemInfo, IInfoProvider<LuceneIndexAssemblyVersionItemInfo>>
{
    /// <summary>
    /// Object type.
    /// </summary>
    public const string OBJECT_TYPE = "kenticolucene.luceneindexassemblyversionitem";


    /// <summary>
    /// Type information.
    /// </summary>
    public static readonly ObjectTypeInfo TYPEINFO = new(typeof(IInfoProvider<LuceneIndexAssemblyVersionItemInfo>), OBJECT_TYPE, "KenticoLucene.LuceneIndexAssemblyVersionItem", nameof(LuceneIndexAssemblyVersionItemID), null, nameof(LuceneIndexAssemblyVersionItemGuid), null, null, null, null, null)
    {
        TouchCacheDependencies = true,
        DependsOn = new List<ObjectDependency>()
        {
            new(nameof(LuceneIndexAssemblyVersionItemIndexItemId), LuceneIndexItemInfo.OBJECT_TYPE, ObjectDependencyEnum.Required),
        },
        ContinuousIntegrationSettings =
        {
            Enabled = true
        }
    };


    /// <summary>
    /// Lucene assembly version item id.
    /// </summary>
    [DatabaseField]
    public virtual int LuceneIndexAssemblyVersionItemID
    {
        get => ValidationHelper.GetInteger(GetValue(nameof(LuceneIndexAssemblyVersionItemID)), 0);
        set => SetValue(nameof(LuceneIndexAssemblyVersionItemID), value);
    }


    /// <summary>
    /// Lucene assembly version item guid.
    /// </summary>
    [DatabaseField]
    public virtual Guid LuceneIndexAssemblyVersionItemGuid
    {
        get => ValidationHelper.GetGuid(GetValue(nameof(LuceneIndexAssemblyVersionItemGuid)), default);
        set => SetValue(nameof(LuceneIndexAssemblyVersionItemGuid), value);
    }


    /// <summary>
    /// Assembly version.
    /// </summary>
    [DatabaseField]
    public virtual string LuceneIndexAssemblyVersionItemAssemblyVersion
    {
        get => ValidationHelper.GetString(GetValue(nameof(LuceneIndexAssemblyVersionItemAssemblyVersion)), String.Empty);
        set => SetValue(nameof(LuceneIndexAssemblyVersionItemAssemblyVersion), value);
    }


    /// <summary>
    /// Lucene index item id.
    /// </summary>
    [DatabaseField]
    public virtual int LuceneIndexAssemblyVersionItemIndexItemId
    {
        get => ValidationHelper.GetInteger(GetValue(nameof(LuceneIndexAssemblyVersionItemIndexItemId)), 0);
        set => SetValue(nameof(LuceneIndexAssemblyVersionItemIndexItemId), value);
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
    /// Creates an empty instance of the <see cref="LuceneIndexAssemblyVersionItemInfo"/> class.
    /// </summary>
    public LuceneIndexAssemblyVersionItemInfo()
        : base(TYPEINFO)
    {
    }


    /// <summary>
    /// Creates a new instances of the <see cref="LuceneIndexAssemblyVersionItemInfo"/> class from the given <see cref="DataRow"/>.
    /// </summary>
    /// <param name="dr">DataRow with the object data.</param>
    public LuceneIndexAssemblyVersionItemInfo(DataRow dr)
        : base(TYPEINFO, dr)
    {
    }
}
