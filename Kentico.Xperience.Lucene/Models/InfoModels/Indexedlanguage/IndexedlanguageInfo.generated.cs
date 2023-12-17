using System;
using System.Data;
using System.Runtime.Serialization;
using System.Collections.Generic;

using CMS;
using CMS.DataEngine;
using CMS.Helpers;
using CMS;

[assembly: RegisterObjectType(typeof(IndexedlanguageInfo), IndexedlanguageInfo.OBJECT_TYPE)]

namespace CMS
{
    /// <summary>
    /// Data container class for <see cref="IndexedlanguageInfo"/>.
    /// </summary>
    [Serializable]
    public partial class IndexedlanguageInfo : AbstractInfo<IndexedlanguageInfo, IIndexedlanguageInfoProvider>
    {
        /// <summary>
        /// Object type.
        /// </summary>
        public const string OBJECT_TYPE = "lucene.indexedlanguage";


        /// <summary>
        /// Type information.
        /// </summary>
#warning "You will need to configure the type info."
        public static readonly ObjectTypeInfo TYPEINFO = new ObjectTypeInfo(typeof(IndexedlanguageInfoProvider), OBJECT_TYPE, "lucene.indexedlanguage", "IndexedLanguageId", null, null, null, null, null, null, null)
        {
            TouchCacheDependencies = true,
            DependsOn = new List<ObjectDependency>()
            {
                new ObjectDependency("LuceneIndexItemId", "IndexitemInfo", ObjectDependencyEnum.Required),
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
        public virtual string languageCode
        {
            get => ValidationHelper.GetString(GetValue(nameof(languageCode)), String.Empty);
            set => SetValue(nameof(languageCode), value);
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
        protected IndexedlanguageInfo(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }


        /// <summary>
        /// Creates an empty instance of the <see cref="IndexedlanguageInfo"/> class.
        /// </summary>
        public IndexedlanguageInfo()
            : base(TYPEINFO)
        {
        }


        /// <summary>
        /// Creates a new instances of the <see cref="IndexedlanguageInfo"/> class from the given <see cref="DataRow"/>.
        /// </summary>
        /// <param name="dr">DataRow with the object data.</param>
        public IndexedlanguageInfo(DataRow dr)
            : base(TYPEINFO, dr)
        {
        }
    }
}