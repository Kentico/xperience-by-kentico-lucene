using CMS.DataEngine;

namespace Kentico.Xperience.Lucene.Models;

/// <summary>
/// Class providing <see cref="ContentTypeItemInfo"/> management.
/// </summary>
[ProviderInterface(typeof(IContenttypeitemInfoProvider))]
public partial class ContentTypeItemInfoProvider : AbstractInfoProvider<ContentTypeItemInfo, ContentTypeItemInfoProvider>, IContenttypeitemInfoProvider
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ContentTypeItemInfoProvider"/> class.
    /// </summary>
    public ContentTypeItemInfoProvider()
        : base(ContentTypeItemInfo.TYPEINFO)
    {
    }
}
