using System;
using System.Data;
using System.Runtime.Serialization;
using System.Collections.Generic;

using CMS;
using CMS.DataEngine;
using CMS.Helpers;
using CMS;

[assembly: RegisterObjectType(typeof(ContenttypeitemInfo), ContenttypeitemInfo.OBJECT_TYPE)]

namespace CMS
{
    /// <summary>
    /// Data container class for <see cref="ContenttypeitemInfo"/>.
    /// </summary>
    [Serializable]
    public partial class ContenttypeitemInfo : AbstractInfo<ContenttypeitemInfo, IContenttypeitemInfoProvider>
    {
        /// <summary>
        /// Object type.
        /// </summary>
        public const string OBJECT_TYPE = "lucene.contenttypeitem";


        /// <summary>
        /// Type information.
        /// </summary>
#warning "You will need to configure the type info."
        public static readonly ObjectTypeInfo TYPEINFO = new ObjectTypeInfo(typeof(ContenttypeitemInfoProvider), OBJECT_TYPE, "lucene.contenttypeitem", "LuceneContentTypeItemId", null, null, "ContentTypeName", null, null, null, null)
        {
            TouchCacheDependencies = true,
            DependsOn = new List<ObjectDependency>()
            {
                new ObjectDependency("LuceneIncludedPathItemId", "IncludedpathitemInfo", ObjectDependencyEnum.Required),
                new ObjectDependency("LuceneIndexItemId", "IndexitemInfo", ObjectDependencyEnum.Required),
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
        protected ContenttypeitemInfo(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }


        /// <summary>
        /// Creates an empty instance of the <see cref="ContenttypeitemInfo"/> class.
        /// </summary>
        public ContenttypeitemInfo()
            : base(TYPEINFO)
        {
        }


        /// <summary>
        /// Creates a new instances of the <see cref="ContenttypeitemInfo"/> class from the given <see cref="DataRow"/>.
        /// </summary>
        /// <param name="dr">DataRow with the object data.</param>
        public ContenttypeitemInfo(DataRow dr)
            : base(TYPEINFO, dr)
        {
        }
    }
}