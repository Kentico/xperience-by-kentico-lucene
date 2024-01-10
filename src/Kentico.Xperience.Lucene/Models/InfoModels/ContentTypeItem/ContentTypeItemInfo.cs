
using System.Data;
using System.Runtime.Serialization;

using CMS;
using CMS.DataEngine;
using CMS.Helpers;

using Kentico.Xperience.Lucene.Models;

[assembly: RegisterObjectType(typeof(ContentTypeItemInfo), ContentTypeItemInfo.OBJECT_TYPE)]

namespace Kentico.Xperience.Lucene.Models;

/// <summary>
/// Data container class for <see cref="ContentTypeItemInfo"/>.
/// </summary>
[Serializable]
public partial class ContentTypeItemInfo : AbstractInfo<ContentTypeItemInfo, IContenttypeitemInfoProvider>
{
    /// <summary>
    /// Object type.
    /// </summary>
    public const string OBJECT_TYPE = "lucene.contenttypeitem";


    /// <summary>
    /// Type information.
    /// </summary>
    public static readonly ObjectTypeInfo TYPEINFO = new(typeof(ContentTypeItemInfoProvider), OBJECT_TYPE, "lucene.contenttypeitem", "LuceneContentTypeItemId", null, null, null, null, null, null, null)
    {
        TouchCacheDependencies = true,
        DependsOn = new List<ObjectDependency>()
        {
            new("LuceneIncludedPathItemId", "IncludedpathitemInfo", ObjectDependencyEnum.Required),
            new("LuceneIndexItemId", "IndexitemInfo", ObjectDependencyEnum.Required),
        },
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
    /// Content type name.
    /// </summary>
    [DatabaseField]
    public virtual string ContentTypeName
    {
        get => ValidationHelper.GetString(GetValue(nameof(ContentTypeName)), String.Empty);
        set => SetValue(nameof(ContentTypeName), value);
    }


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
    /// Lucene index item id.
    /// </summary>
    [DatabaseField]
    public virtual int LuceneIndexItemId
    {
        get => ValidationHelper.GetInteger(GetValue(nameof(LuceneIndexItemId)), 0);
        set => SetValue(nameof(LuceneIndexItemId), value);
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
    /// Constructor for de-serialization.
    /// </summary>
    /// <param name="info">Serialization info.</param>
    /// <param name="context">Streaming context.</param>
    protected ContentTypeItemInfo(SerializationInfo info, StreamingContext context)
        : base(info, context)
    {
    }


    /// <summary>
    /// Creates an empty instance of the <see cref="ContentTypeItemInfo"/> class.
    /// </summary>
    public ContentTypeItemInfo()
        : base(TYPEINFO)
    {
    }


    /// <summary>
    /// Creates a new instances of the <see cref="ContentTypeItemInfo"/> class from the given <see cref="DataRow"/>.
    /// </summary>
    /// <param name="dr">DataRow with the object data.</param>
    public ContentTypeItemInfo(DataRow dr)
        : base(TYPEINFO, dr)
    {
    }
}