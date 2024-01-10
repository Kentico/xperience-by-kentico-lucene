using System.Data;
using System.Runtime.Serialization;

using CMS;
using CMS.DataEngine;
using CMS.Helpers;
using Kentico.Xperience.Lucene.Models;

[assembly: RegisterObjectType(typeof(IndexItemInfo), IndexItemInfo.OBJECT_TYPE)]

namespace Kentico.Xperience.Lucene.Models
{
    /// <summary>
    /// Data container class for <see cref="IndexItemInfo"/>.
    /// </summary>
    [Serializable]
    public partial class IndexItemInfo : AbstractInfo<IndexItemInfo, IIndexItemInfoProvider>
    {
        /// <summary>
        /// Object type.
        /// </summary>
        public const string OBJECT_TYPE = "lucene.indexitem";


        /// <summary>
        /// Type information.
        /// </summary>
        public static readonly ObjectTypeInfo TYPEINFO = new(typeof(IndexItemInfoProvider), OBJECT_TYPE, "lucene.indexitem", "LuceneIndexItemId", null, null, "IndexName", null, null, null, null)
        {
            TouchCacheDependencies = true,
        };


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
        /// Index name.
        /// </summary>
        [DatabaseField]
        public virtual string IndexName
        {
            get => ValidationHelper.GetString(GetValue(nameof(IndexName)), String.Empty);
            set => SetValue(nameof(IndexName), value);
        }


        /// <summary>
        /// Channel name.
        /// </summary>
        [DatabaseField]
        public virtual string ChannelName
        {
            get => ValidationHelper.GetString(GetValue(nameof(ChannelName)), String.Empty);
            set => SetValue(nameof(ChannelName), value);
        }


        /// <summary>
        /// Strategy name.
        /// </summary>
        [DatabaseField]
        public virtual string StrategyName
        {
            get => ValidationHelper.GetString(GetValue(nameof(StrategyName)), String.Empty);
            set => SetValue(nameof(StrategyName), value);
        }


        /// <summary>
        /// Rebuild hook.
        /// </summary>
        [DatabaseField]
        public virtual string RebuildHook
        {
            get => ValidationHelper.GetString(GetValue(nameof(RebuildHook)), String.Empty);
            set => SetValue(nameof(RebuildHook), value, String.Empty);
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
        protected IndexItemInfo(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }


        /// <summary>
        /// Creates an empty instance of the <see cref="IndexItemInfo"/> class.
        /// </summary>
        public IndexItemInfo()
            : base(TYPEINFO)
        {
        }


        /// <summary>
        /// Creates a new instances of the <see cref="IndexItemInfo"/> class from the given <see cref="DataRow"/>.
        /// </summary>
        /// <param name="dr">DataRow with the object data.</param>
        public IndexItemInfo(DataRow dr)
            : base(TYPEINFO, dr)
        {
        }
    }
}
