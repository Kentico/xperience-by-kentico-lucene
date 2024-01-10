using System.Data;
using System.Runtime.Serialization;

using CMS;
using CMS.DataEngine;
using CMS.Helpers;

using Kentico.Xperience.Lucene.Models;

[assembly: RegisterObjectType(typeof(IndexedLanguageInfo), IndexedLanguageInfo.OBJECT_TYPE)]

namespace Kentico.Xperience.Lucene.Models
{
    /// <summary>
    /// Data container class for <see cref="IndexedLanguageInfo"/>.
    /// </summary>
    [Serializable]
    public partial class IndexedLanguageInfo : AbstractInfo<IndexedLanguageInfo, IIndexedLanguageInfoProvider>
    {
        /// <summary>
        /// Object type.
        /// </summary>
        public const string OBJECT_TYPE = "lucene.indexedlanguage";


        /// <summary>
        /// Type information.
        /// </summary>
        public static readonly ObjectTypeInfo TYPEINFO = new(typeof(IndexedLanguageInfoProvider), OBJECT_TYPE, "lucene.indexedlanguage", "IndexedLanguageId", null, null, null, null, null, null, null)
        {
            TouchCacheDependencies = true,
            DependsOn = new List<ObjectDependency>()
            {
                new("LuceneIndexItemId", "IndexitemInfo", ObjectDependencyEnum.Required),
            },
        };


        /// <summary>
        /// Indexed language id.
        /// </summary>
        [DatabaseField]
        public virtual int IndexedLanguageId
        {
            get => ValidationHelper.GetInteger(GetValue(nameof(IndexedLanguageId)), 0);
            set => SetValue(nameof(IndexedLanguageId), value);
        }


        /// <summary>
        /// Code.
        /// </summary>
        [DatabaseField]
        public virtual string LanguageCode
        {
            get => ValidationHelper.GetString(GetValue(nameof(LanguageCode)), String.Empty);
            set => SetValue(nameof(LanguageCode), value);
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
        protected IndexedLanguageInfo(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }


        /// <summary>
        /// Creates an empty instance of the <see cref="IndexedLanguageInfo"/> class.
        /// </summary>
        public IndexedLanguageInfo()
            : base(TYPEINFO)
        {
        }


        /// <summary>
        /// Creates a new instances of the <see cref="IndexedLanguageInfo"/> class from the given <see cref="DataRow"/>.
        /// </summary>
        /// <param name="dr">DataRow with the object data.</param>
        public IndexedLanguageInfo(DataRow dr)
            : base(TYPEINFO, dr)
        {
        }
    }
}
