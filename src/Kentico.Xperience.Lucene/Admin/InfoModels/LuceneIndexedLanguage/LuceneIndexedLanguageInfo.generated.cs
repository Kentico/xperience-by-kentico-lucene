using System.Data;
using System.Runtime.Serialization;

using CMS;
using CMS.DataEngine;
using CMS.Helpers;

using Kentico.Xperience.Lucene.Models;

[assembly: RegisterObjectType(typeof(LuceneIndexedLanguageInfo), LuceneIndexedLanguageInfo.OBJECT_TYPE)]

namespace Kentico.Xperience.Lucene.Models
{
    /// <summary>
    /// Data container class for <see cref="LuceneIndexedLanguageInfo"/>.
    /// </summary>
    [Serializable]
    public partial class LuceneIndexedLanguageInfo : AbstractInfo<LuceneIndexedLanguageInfo, ILuceneIndexedLanguageInfoProvider>
    {
        /// <summary>
        /// Object type.
        /// </summary>
        public const string OBJECT_TYPE = "lucene.luceneindexedlanguage";


        /// <summary>
        /// Type information.
        /// </summary>
        public static readonly ObjectTypeInfo TYPEINFO = new(typeof(LuceneIndexedLanguageInfoProvider), OBJECT_TYPE, "lucene.LuceneIndexedLanguage", "LuceneIndexedLanguageId", null, null, null, null, null, null, null)
        {
            TouchCacheDependencies = true,
            DependsOn = new List<ObjectDependency>()
            {
                new("LuceneIndexItemId", "LuceneIndexitemInfo", ObjectDependencyEnum.Required),
            },
        };


        /// <summary>
        /// Indexed language id.
        /// </summary>
        [DatabaseField]
        public virtual int LuceneIndexedLanguageId
        {
            get => ValidationHelper.GetInteger(GetValue(nameof(LuceneIndexedLanguageId)), 0);
            set => SetValue(nameof(LuceneIndexedLanguageId), value);
        }


        /// <summary>
        /// Code.
        /// </summary>
        [DatabaseField]
        public virtual string LuceneIndexedLanguageName
        {
            get => ValidationHelper.GetString(GetValue(nameof(LuceneIndexedLanguageName)), String.Empty);
            set => SetValue(nameof(LuceneIndexedLanguageName), value);
        }


        /// <summary>
        /// Lucene index item id.
        /// </summary>
        [DatabaseField]
        public virtual int LuceneIndexedLanguageIndexItemId
        {
            get => ValidationHelper.GetInteger(GetValue(nameof(LuceneIndexedLanguageIndexItemId)), 0);
            set => SetValue(nameof(LuceneIndexedLanguageIndexItemId), value);
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
        protected LuceneIndexedLanguageInfo(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }


        /// <summary>
        /// Creates an empty instance of the <see cref="LuceneIndexedLanguageInfo"/> class.
        /// </summary>
        public LuceneIndexedLanguageInfo()
            : base(TYPEINFO)
        {
        }


        /// <summary>
        /// Creates a new instances of the <see cref="LuceneIndexedLanguageInfo"/> class from the given <see cref="DataRow"/>.
        /// </summary>
        /// <param name="dr">DataRow with the object data.</param>
        public LuceneIndexedLanguageInfo(DataRow dr)
            : base(TYPEINFO, dr)
        {
        }
    }
}
